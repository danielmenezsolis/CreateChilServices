using System;
using System.AddIn;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CreateChilServices.SOAPICCS;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;

namespace CreateChilServices
{
    public class WorkspaceRibbonAddIn : Panel, IWorkspaceRibbonButton
    {
        private IRecordContext RecordContext { get; set; }
        private IGlobalContext GlobalContext { get; set; }
        public RightNowSyncPortClient clientORN { get; private set; }
        public IIncident Incident { get; set; }
        public int IncidentID { get; set; }
        public int Packages { get; set; }
        public int BabyPackages { get; set; }
        public int BabyPayables { get; set; }
        public string InformativoPadre { get; set; }
        public List<WHours> WHoursList { get; set; }
        public string SRType { get; set; }
        public DateTime ATA { get; set; }
        public DateTime ATD { get; set; }
        public string Currency { get; set; }
        public string Supplier { get; set; }
        public string OUM { get; set; }
        public string ICAOId { get; set; }
        public string CustomerName { get; set; }
        public string ParentItemDescription { get; set; }
        public string ParentId { get; set; }

        //Obtención de todos los precios
        public string main = "MAIN";
        public int fbo = 0;
        public string airport = "AIRPORT";
        string[] arridepart;
        public int IdItinerary = 0;
        public string claseCliente = "CLASE";
        public string pswCPQ = "";

        public ClaseParaPaquetes.RootObject PaquetesCostos { get; set; }

        public WorkspaceRibbonAddIn(bool inDesignMode, IRecordContext RecordContext, IGlobalContext GlobalContext)
        {
            if (inDesignMode == false)
            {
                this.GlobalContext = GlobalContext;
                this.RecordContext = RecordContext;
            }
        }
        public new void Click()
        {
            pswCPQ = getPassword("CPQ");
            try
            {
                DialogResult dr = MessageBox.Show("Would you like to create child services from Fuel/Packages?",
                          "Confirm", MessageBoxButtons.YesNo);
                switch (dr)
                {
                    case DialogResult.Yes:
                        BabyPackages = 0;
                        Packages = 0;
                        BabyPayables = 0;
                        if (Init())
                        {
                            Incident = (IIncident)RecordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                            IncidentID = Incident.ID;
                            IList<ICfVal> IncCustomFieldList = Incident.CustomField;
                            if (IncCustomFieldList != null)
                            {
                                foreach (ICfVal inccampos in IncCustomFieldList)
                                {
                                    if (inccampos.CfId == 58)
                                    {
                                        CustomerName = inccampos.ValStr;
                                    }
                                }
                            }
                            ICAOId = getICAODesi(IncidentID);
                            SRType = GetSRType();
                            //GetDeleteComponents();
                            Cursor.Current = Cursors.WaitCursor;
                            var watch = Stopwatch.StartNew();
                            CreateChildComponents();
                            watch.Stop();
                            var elapsedMs = watch.Elapsed;
                            GlobalContext.LogMessage("CreateChildComponents: " + elapsedMs.TotalSeconds.ToString() + " Secs");

                            watch = Stopwatch.StartNew();
                            if (Packages > 0)
                            {
                                // LLEGADA Y SALIDA
                                arridepart = GetCountryLookItinerary(IdItinerary);
                                claseCliente = GetClase();
                                // FBO
                                if (SRType == "FBO")
                                {
                                    fbo = 1;
                                }
                                else if (SRType == "FCC")
                                {
                                    fbo = GetFBOValue(IdItinerary.ToString());
                                }
                                // MAIN HOUR
                                GetItineraryHours(IdItinerary);
                                main = GetMainHour(ATA.ToString(), ATD.ToString());

                                UpdatePackageCost();
                                //UpdatePayables();
                                MessageBox.Show("Packages Found: " + Packages + "\n" + "Child Services Created: " + BabyPackages + "\n" + "Child Payables Created: " + BabyPayables);
                            }
                            else
                            {
                                MessageBox.Show("Any new packages wasn't found.");
                            }
                            RecordContext.ExecuteEditorCommand(EditorCommand.Save);
                            watch.Stop();
                            elapsedMs = watch.Elapsed;
                            //UpdatePayables();
                            GlobalContext.LogMessage("Packages & Message: " + elapsedMs.TotalSeconds.ToString() + " Secs");
                            Cursor.Current = Cursors.Default;

                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en Click: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        private void UpdatePayables()
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Sum(TicketAmount),Services FROM CO.Payables WHERE Services.Incident =" + IncidentID + " AND Services.Paquete = '1' GROUP BY Services";
                GlobalContext.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        double amount = Convert.ToDouble(substrings[0]);
                        string service = substrings[1];
                        UpdatePaxPrice(service, amount);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdpatePayables" + ex.Message + " Det :" + ex.StackTrace);
            }
        }
        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(GlobalContext.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                clientORN = new RightNowSyncPortClient(binding, endPointAddr);
                BindingElementCollection elements = clientORN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientORN.Endpoint.Binding = new CustomBinding(elements);
                GlobalContext.PrepareConnectSession(clientORN.ChannelFactory);
                if (clientORN != null)
                {
                    result = true;
                }
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en INIT: " + ex.Message);
                return false;
            }
        }
        public string getICAODesi(int Incident)
        {
            string Icao = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.co.Aircraft.AircraftType1.ICAODesignator  FROM Incident WHERE ID =" + Incident;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Icao = data;
                }
            }
            return Icao;
        }
        public void GetDeleteComponents()
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID FROM CO.Services WHERE ManualCreated = '1' AND Incident = " + IncidentID;
                List<string> id = new List<string>();
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 100, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            id.Add(data);
                        }
                    }
                }
                watch.Stop();
                var elapsedMs = watch.Elapsed;
                GlobalContext.LogMessage("GetComponents: " + elapsedMs.TotalSeconds.ToString() + " Secs");
                DeleteServices(id);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void DeleteServices(List<string> id)
        {
            try
            {

                var client = new RestClient("https://iccsmx.custhelp.com/");
                foreach (string i in id)
                {

                    var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + i, Method.DELETE);
                    request.RequestFormat = DataFormat.Json;
                    request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                    request.AddHeader("X-HTTP-Method-Override", "DELETE");
                    request.AddHeader("OSvC-CREST-Application-Context", "Delete Service");
                    IRestResponse response = client.Execute(request);
                    /*
                     * var content = response.Content;
                    if (String.IsNullOrEmpty(content))
                    {
                        MessageBox.Show(content);
                    }*/
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete: " + ex.InnerException.ToString());
            }
        }
        public void CreateChildComponents()
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Airport,ItemNumber,Itinerary,Informativo,ItemDescription FROM CO.Services WHERE (Paquete = '1' AND Broken = 0) AND COMPONENTE IS NULL AND  Incident =  " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            Packages++;
                            ComponentChild component = new ComponentChild();
                            Char delimiter = '|';
                            String[] substrings = data.Split(delimiter);
                            component.ID = Convert.ToInt32(substrings[0]);
                            component.Airport = substrings[1];
                            component.ItemNumber = substrings[2];
                            component.Itinerary = string.IsNullOrEmpty(substrings[3]) ? 0 : Convert.ToInt32(substrings[3]);
                            InformativoPadre = substrings[4];
                            ParentItemDescription = substrings[5];
                            ParentId = substrings[0];
                            BrokenPackage(ParentId);
                            if (CustomerName.Contains("NETJETS") && (ParentItemDescription.Contains("(NJ)")))
                            {
                                InformativeNJ(ParentId);
                            }
                            NGetComponents(component);
                            // ITINERARIO
                            if (IdItinerary == 0)
                            {
                                IdItinerary = component.Itinerary;
                            }
                            // AEROPUERTO
                            if (airport == "AIRPORT")
                            {
                                airport = component.Airport;
                            }
                            //GetComponents(component);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("CreateChildComponents: " + ex.Message + "Det: " + ex.StackTrace);
            }
        }
        public void NGetComponents(ComponentChild componentparent)
        {
            try
            {
                string envelope = "<soap:Envelope " +
                "	xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"" +
                "	xmlns:pub=\"http://xmlns.oracle.com/oxp/service/PublicReportService\">" +
                 "<soap:Header/>" +
                "<soap:Body>" +
                "<pub:runReport>" +
                "<pub:reportRequest>" +
                "<pub:attributeFormat>xml</pub:attributeFormat>" +
                "<pub:attributeLocale></pub:attributeLocale>" +
                "<pub:attributeTemplate></pub:attributeTemplate>" +
                "<pub:reportAbsolutePath>/Custom/Integracion/XX_PAQUETES_CX_REP.xdo</pub:reportAbsolutePath>" +
                "<pub:sizeOfDataChunkDownload>-1</pub:sizeOfDataChunkDownload>" +
                "<pub:parameterNameValues>" +
                                "<pub:item>" +
                                    "<pub:name>pAereo</pub:name> " +
                                    "<pub:values> " +
                                        "<pub:item>IO_AEREO_" + componentparent.Airport + "</pub:item> " +
                                    "</pub:values> " +
                                "</pub:item> " +
                                "<pub:item> " +
                                   "<pub:name>pItem</pub:name>" +
                                    "<pub:values>" +
                                        "<pub:item>" + componentparent.ItemNumber + "</pub:item>" +
                                    "</pub:values>" +
                                "</pub:item>" +
                            "</pub:parameterNameValues>" +
                "</pub:reportRequest>" +
                "</pub:runReport>" +
                "</soap:Body>" +
                "</soap:Envelope>";
                GlobalContext.LogMessage(envelope);
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/xmlpserver/services/ExternalReportWSSService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";

                request.ContentType = "application/soap+xml; charset=UTF-8;action=\"\"";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                //request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/findOrganizationProfile");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/oxp/service/PublicReportService");

                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:runReportReturn", nms);
                        if (desiredNode.HasChildNodes)
                        {
                            for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                            {
                                if (desiredNode.ChildNodes[i].LocalName == "reportBytes")
                                {
                                    byte[] data = Convert.FromBase64String(desiredNode.ChildNodes[i].InnerText);
                                    string decodedString = Encoding.UTF8.GetString(data);
                                    XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(decodedString));
                                    reader.Read();
                                    XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS));
                                    DATA_DS res = (DATA_DS)serializer.Deserialize(reader);
                                    if (res.PAQUETE.NIVEL_1 != null)
                                    {
                                        foreach (NIVEL_1 n1 in res.PAQUETE.NIVEL_1)
                                        {
                                            if (string.IsNullOrEmpty(n1.DESCRIPTION_HIJO))
                                            {
                                                continue;
                                            }
                                            ComponentChild componentchild1 = new ComponentChild()
                                            {
                                                ParentPaxId = componentparent.ID,
                                                ServiceParent = componentparent.ID,
                                                Airport = componentparent.Airport,
                                                Incident = IncidentID,

                                                Componente = "1",
                                                MCreated = "1",
                                                Itinerary = componentparent.Itinerary,
                                                ItemNumber = n1.ITEM_HIJO,
                                                ItemDescription = n1.DESCRIPTION_HIJO,
                                                ParticipacionCobro = n1.XX_PARTICIPACION_COBRO_HIJO == "SI" ? "1" : "0",
                                                CobroParticipacionNj = n1.XX_COBRO_PARTICIPACION_NJ_HIJO == "SI" ? "1" : "0",
                                                Pagos = n1.XX_PAGOS_HIJO,
                                                ClasificacionPagos = n1.XX_CLASIFICACION_PAGO_HIJO,
                                                Informativo = n1.XX_INFORMATIVO_HIJO == "SI" ? "1" : "0",
                                                Paquete = n1.XX_PAQUETE_INV_HIJO == "SI" ? "1" : "0",
                                                Categories = string.Join("+", n1.CAT_NIVEL_1.Select(o => o.CATALOG_CODE).ToArray()),
                                            };

                                            int id1 = NInsertComponent(componentchild1);

                                            if (n1.PAQUETE_2 != null)
                                            {
                                                Packages++;
                                                List<NIVEL_2> nivel2 = n1.PAQUETE_2.NIVEL_2;
                                                foreach (NIVEL_2 n2 in nivel2)
                                                {

                                                    ComponentChild componentchild2 = new ComponentChild()
                                                    {
                                                        ParentPaxId = id1,
                                                        ServiceParent = id1,
                                                        Airport = componentparent.Airport,
                                                        Incident = IncidentID,
                                                        Componente = "1",
                                                        MCreated = "1",
                                                        Itinerary = componentparent.Itinerary,
                                                        ItemNumber = n2.ITEM_HIJO,
                                                        ItemDescription = n2.DESCRIPTION_HIJO,
                                                        ParticipacionCobro = n2.XX_PARTICIPACION_COBRO_HIJO == "SI" ? "1" : "0",
                                                        CobroParticipacionNj = n2.XX_COBRO_PARTICIPACION_NJ_HIJO == "SI" ? "1" : "0",
                                                        Pagos = n2.XX_PAGOS_HIJO,
                                                        ClasificacionPagos = n2.XX_CLASIFICACION_PAGO_HIJO,
                                                        Informativo = n2.XX_INFORMATIVO_HIJO == "SI" ? "1" : "0",
                                                        Paquete = n2.XX_PAQUETE_INV_HIJO == "SI" ? "1" : "0",
                                                        Categories = string.Join("+", n2.CAT_NIVEL_2.Select(o => o.CATALOG_CODE).ToArray())
                                                    };


                                                    int id2 = NInsertComponent(componentchild2);


                                                    if (n2.PAQUETE_3 != null)
                                                    {
                                                        Packages++;
                                                        List<NIVEL_3> nivel3 = n2.PAQUETE_3.NIVEL_3;
                                                        foreach (NIVEL_3 n3 in nivel3)
                                                        {

                                                            ComponentChild componentchild3 = new ComponentChild()
                                                            {
                                                                ParentPaxId = id2,
                                                                ServiceParent = id2,
                                                                Airport = componentparent.Airport,
                                                                Incident = IncidentID,
                                                                Componente = "1",
                                                                MCreated = "1",
                                                                Itinerary = componentparent.Itinerary,
                                                                ItemNumber = n3.ITEM_HIJO,
                                                                ItemDescription = n3.DESCRIPTION_HIJO,
                                                                ParticipacionCobro = n3.XX_PARTICIPACION_COBRO_HIJO == "SI" ? "1" : "0",
                                                                CobroParticipacionNj = n3.XX_COBRO_PARTICIPACION_NJ_HIJO == "SI" ? "1" : "0",
                                                                Pagos = n3.XX_PAGOS_HIJO,
                                                                ClasificacionPagos = n3.XX_CLASIFICACION_PAGO_HIJO,
                                                                Informativo = n3.XX_INFORMATIVO_HIJO == "SI" ? "1" : "0",
                                                                Paquete = n3.XX_PAQUETE_INV_HIJO == "SI" ? "1" : "0",
                                                                Categories = string.Join("+", n3.CAT_NIVEL_3.Select(o => o.CATALOG_CODE).ToArray())
                                                            };

                                                            int id3 = NInsertComponent(componentchild3);

                                                            if (n3.PAQUETE_4 != null)
                                                            {
                                                                Packages++;
                                                                List<NIVEL_4> nivel4 = n3.PAQUETE_4.NIVEL_4;
                                                                foreach (NIVEL_4 n4 in nivel4)
                                                                {

                                                                    ComponentChild componentchild4 = new ComponentChild()
                                                                    {
                                                                        ParentPaxId = id1,
                                                                        ServiceParent = id1,
                                                                        Airport = componentparent.Airport,
                                                                        Incident = IncidentID,
                                                                        Componente = "1",
                                                                        MCreated = "1",
                                                                        Itinerary = componentparent.Itinerary,
                                                                        ItemNumber = n4.ITEM_HIJO,
                                                                        ItemDescription = n4.DESCRIPTION_HIJO,
                                                                        ParticipacionCobro = n4.XX_PARTICIPACION_COBRO_HIJO == "SI" ? "1" : "0",
                                                                        CobroParticipacionNj = n4.XX_COBRO_PARTICIPACION_NJ_HIJO == "SI" ? "1" : "0",
                                                                        Pagos = n4.XX_PAGOS_HIJO,
                                                                        ClasificacionPagos = n4.XX_CLASIFICACION_PAGO_HIJO,
                                                                        Informativo = n4.XX_INFORMATIVO_HIJO == "SI" ? "1" : "0",
                                                                        Paquete = n4.XX_PAQUETE_INV_HIJO == "SI" ? "1" : "0",
                                                                        Categories = string.Join("+", n4.CAT_NIVEL_4.Select(o => o.CATALOG_CODE).ToArray())
                                                                    };

                                                                    int id4 = NInsertComponent(componentchild4);
                                                                    if (n4.PAQUETE_5 != null)
                                                                    {
                                                                        Packages++;
                                                                        List<NIVEL_5> nivel5 = n4.PAQUETE_5.NIVEL_5;
                                                                        foreach (NIVEL_5 n5 in nivel5)
                                                                        {
                                                                            ComponentChild componentchild5 = new ComponentChild()
                                                                            {
                                                                                ParentPaxId = id4,
                                                                                ServiceParent = id4,
                                                                                Airport = componentparent.Airport,
                                                                                Incident = IncidentID,
                                                                                Componente = "1",
                                                                                MCreated = "1",
                                                                                Itinerary = componentparent.Itinerary,
                                                                                ItemNumber = n5.ITEM_HIJO,
                                                                                ItemDescription = n5.DESCRIPTION_HIJO,
                                                                                ParticipacionCobro = n5.XX_PARTICIPACION_COBRO_HIJO == "SI" ? "1" : "0",
                                                                                CobroParticipacionNj = n5.XX_COBRO_PARTICIPACION_NJ_HIJO == "SI" ? "1" : "0",
                                                                                Pagos = n5.XX_PAGOS_HIJO,
                                                                                ClasificacionPagos = n5.XX_CLASIFICACION_PAGO_HIJO,
                                                                                Informativo = n5.XX_INFORMATIVO_HIJO == "SI" ? "1" : "0",
                                                                                Paquete = n5.XX_PAQUETE_INV_HIJO == "SI" ? "1" : "0",
                                                                                Categories = string.Join("+", n5.CAT_NIVEL_5.Select(o => o.CATALOG_CODE).ToArray())
                                                                            };
                                                                            int id5 = NInsertComponent(componentchild5);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("GetNComponents" + e.Message + "Det:" + e.StackTrace);
            }
        }

        public void GetComponents(ComponentChild component)
        {
            try
            {
                string envelope = "<soapenv:Envelope" +
                 "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                 "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/types/\"" +
                 "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                 "<soapenv:Header/>" +
                 "<soapenv:Body>" +
                 "<typ:findStructure>" +
                 "<typ:findCriteria>" +
                 "<typ1:fetchStart>0</typ1:fetchStart>" +
                 "<typ1:fetchSize>-1</typ1:fetchSize>" +
                 "<typ1:filter>" +
                 "<typ1:group>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>ItemNumber</typ1:attribute>" +
                 "<typ1:operator>CONTAINS</typ1:operator>" +
                 "<typ1:value>" + component.ItemNumber + "</typ1:value>" +
                 "</typ1:item>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                 "<typ1:operator>CONTAINS</typ1:operator>" +
                 "<typ1:value>" + component.Airport + "</typ1:value>" +
                 "</typ1:item>" +
                 "</typ1:group> " +
                 "</typ1:filter>" +
                 "<typ1:findAttribute>Component</typ1:findAttribute>" +
                 "<typ1:childFindCriteria>" +
                 "<typ1:findAttribute>ComponentItemNumber</typ1:findAttribute>" +
                 "<typ1:childAttrName>Component</typ1:childAttrName>" +
                 "</typ1:childFindCriteria>" +
                 "</typ:findCriteria>" +
                 "<typ:findControl>" +
                 "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                 "</typ:findControl>" +
                 "</typ:findStructure>" +
                 "</soapenv:Body>" +
                 "</soapenv:Envelope>";
                //GlobalContext.LogMessage(envelope);
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/StructureServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/findStructure");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                List<ComponentChild> components = new List<ComponentChild>();
                using (WebResponse responseComponent = request.GetResponse())
                {
                    using (Stream stream = responseComponent.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/");
                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns1:Component", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            ComponentChild componentchild = new ComponentChild();
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "Component")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "ComponentItemNumber")
                                        {
                                            componentchild.ParentPaxId = component.ID;
                                            componentchild.ServiceParent = component.ID;
                                            componentchild.Airport = component.Airport;
                                            componentchild.Incident = IncidentID;
                                            componentchild.Componente = "1";
                                            componentchild.MCreated = "1";
                                            componentchild.ItemNumber = nodeValue.InnerText;
                                            componentchild.Itinerary = component.Itinerary;
                                            componentchild.Categories = GetCategories(componentchild.ItemNumber, componentchild.Airport);
                                        }
                                    }
                                }
                            }
                            components.Add(componentchild);
                        }
                        responseComponent.Close();
                    }
                }

                if (components.Count > 0)
                {
                    foreach (ComponentChild comp in components)
                    {
                        ComponentChild comp2 = new ComponentChild();
                        comp2 = GetComponentData(comp);
                        if (!String.IsNullOrEmpty(comp2.ItemDescription))
                        {
                            InsertComponent(comp2);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("GetComponents" + e.Message + "Det:" + e.StackTrace);
            }
        }
        public string GetCategories(string ItemN, string Airport)
        {
            try
            {
                string cats = "";
                string envelope = "<soapenv:Envelope" +
                                "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                                "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                                "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                                "<soapenv:Header/>" +
                                "<soapenv:Body>" +
                       "<typ:findItem>" +
                           "<typ:findCriteria>" +
                               "<typ1:fetchStart>0</typ1:fetchStart>" +
                               "<typ1:fetchSize>-1</typ1:fetchSize>" +
                               "<typ1:filter>" +
                                   "<typ1:group>" +
                                       "<typ1:item>" +
                                           "<typ1:conjunction>And</typ1:conjunction>" +
                                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                           "<typ1:attribute>ItemNumber</typ1:attribute>" +
                                           "<typ1:operator>=</typ1:operator>" +
                                           "<typ1:value>" + ItemN + "</typ1:value>" +
                                       "</typ1:item>" +
                                       "<typ1:item>" +
                                           "<typ1:conjunction>And</typ1:conjunction>" +
                                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                           "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                                           "<typ1:operator>=</typ1:operator>" +
                                           "<typ1:value>IO_AEREO_" + Airport + "</typ1:value>" +
                                       "</typ1:item>" +
                                   "</typ1:group>" +
                               "</typ1:filter>" +
                               "<typ1:findAttribute>ItemCategory</typ1:findAttribute>" +
                           "</typ:findCriteria>" +
                           "<typ:findControl>" +
                               "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                           "</typ:findControl>" +
                       "</typ:findItem>" +
                    "</soapenv:Body>" +
                    "</soapenv:Envelope>";

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/fscmService/ItemServiceV2");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse responseComponent = request.GetResponse())
                {
                    using (Stream stream = responseComponent.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");
                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns1:ItemCategory", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            ComponentChild component = new ComponentChild();
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "ItemCategory")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "CategoryName")
                                        {
                                            cats += nodeValue.InnerText + "+";
                                        }
                                    }
                                }
                            }
                        }
                        responseComponent.Close();
                    }
                }
                return cats;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetCategories:" + ex.Message + "Det: " + ex.StackTrace);
                return "";
            }
        }
        public ComponentChild GetComponentData(ComponentChild component)
        {
            try
            {
                string envelope = "<soapenv:Envelope" +
                                          "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                                          "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                                          "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                                          "<soapenv:Header/>" +
                                          "<soapenv:Body>" +
                                          "<typ:findItem>" +
                                          "<typ:findCriteria>" +
                                          "<typ1:fetchStart>0</typ1:fetchStart>" +
                                          "<typ1:fetchSize>-1</typ1:fetchSize>" +
                                          "<typ1:filter>" +
                                          "<typ1:group>" +
                                          "<typ1:item>" +
                                          "<typ1:conjunction>And</typ1:conjunction>" +
                                          "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                          "<typ1:attribute>ItemNumber</typ1:attribute>" +
                                          "<typ1:operator>=</typ1:operator>" +
                                          "<typ1:value>" + component.ItemNumber + "</typ1:value>" +
                                          "</typ1:item>" +
                                          "<typ1:item>" +
                                          "<typ1:conjunction>And</typ1:conjunction>" +
                                          "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                          "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                                          "<typ1:operator>=</typ1:operator>" +
                                          "<typ1:value>IO_AEREO_" + component.Airport + "</typ1:value>" +
                                          "</typ1:item>" +
                                          /*  "<typ1:item>" +
                                      "<typ1:conjunction>And</typ1:conjunction>" +
                                      "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                      "<typ1:attribute>ItemCategory</typ1:attribute>" +
                                      "<typ1:nested>" +
                                      "<typ1:group>" +
                                      "<typ1:item>" +
                                      "<typ1:conjunction>And</typ1:conjunction>" +
                                      "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                      "<typ1:attribute>CategoryName</typ1:attribute>" +
                                      "<typ1:operator>=</typ1:operator>" +
                                      "<typ1:value>FCC</typ1:value>" +
                                      "</typ1:item>" +
                                      "</typ1:group>" +
                                      "</typ1:nested>" +
                                      "</typ1:item>" +*/
                                          "</typ1:group>" +
                                          "</typ1:filter>" +
                                          "<typ1:findAttribute>ItemDescription</typ1:findAttribute>" +
                                          "<typ1:findAttribute>ItemDFF</typ1:findAttribute>" +
                                          "</typ:findCriteria>" +
                                          "<typ:findControl>" +
                                          "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                                          "</typ:findControl>" +
                                          "</typ:findItem>" +
                                          "</soapenv:Body>" +
                                          "</soapenv:Envelope>";
                //GlobalContext.LogMessage(envelope);

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/findItem");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse responseComponentGet = request.GetResponse())
                {
                    using (Stream stream = responseComponentGet.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns0", "http://xmlns.oracle.com/adf/svc/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");

                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns0:Value", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "Value")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "ItemDescription")
                                        {
                                            component.ItemDescription = nodeValue.InnerText.Trim().Replace("/", "");
                                        }
                                        if (nodeValue.LocalName == "ItemDFF")
                                        {
                                            XmlNodeList nodeListDeff = nodeValue.ChildNodes;
                                            {
                                                foreach (XmlNode nodeDeff in nodeListDeff)
                                                {
                                                    if (nodeDeff.LocalName == "xxParticipacionCobro")
                                                    {
                                                        component.ParticipacionCobro = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                    if (nodeDeff.LocalName == "xxCobroParticipacionNj")
                                                    {
                                                        component.CobroParticipacionNj = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                    if (nodeDeff.LocalName == "xxPagos")
                                                    {
                                                        component.Pagos = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "xxClasificacionPago")
                                                    {
                                                        component.ClasificacionPagos = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "cuentaGastoCx")
                                                    {
                                                        component.CuentaGasto = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "xxInformativo")
                                                    {
                                                        component.Informativo = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                    if (nodeDeff.LocalName == "xxPaqueteInv")
                                                    {
                                                        component.Paquete = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                }
                                            }

                                        }

                                    }
                                }
                            }
                        }
                    }
                    responseComponentGet.Close();
                }

                component.MCreated = "1";
                if (component.ParentPaxId != Convert.ToDouble(ParentId) && CustomerName.Contains("NETJETS"))
                {
                    component.Informativo = "1";
                }
                if (component.ParentPaxId == Convert.ToDouble(ParentId) && ParentItemDescription.Contains("NJ") && CustomerName.Contains("NETJETS"))
                {
                    component.Componente = "0";
                }
                if (component.ItemNumber == "ASFIEAP357" && CustomerName.Contains("NETJETS"))
                {
                    component.Informativo = "1";
                }
                /*
                if (InformativoPadre == "1")
                {
                    component.Informativo = "1";
                }
                */
                return component;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetComponentData: " + ex.Message + "Det: " + ex.StackTrace);
                return null;
            }
        }
        public int NInsertComponent(ComponentChild component)
        {
            try
            {
                int insert = 0;
                component.MCreated = "1";
                if (component.ParentPaxId != Convert.ToDouble(ParentId) && CustomerName.Contains("NETJETS"))
                {
                    component.Informativo = "1";
                }
                if (component.ParentPaxId == Convert.ToDouble(ParentId) && ParentItemDescription.Contains("NJ") && CustomerName.Contains("NETJETS"))
                {
                    component.Componente = "0";
                }
                if (component.ItemNumber == "ASFIEAP357" && CustomerName.Contains("NETJETS"))
                {
                    component.Informativo = "1";
                }


                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                string body = "{";
                body += "\"Airport\":\"" + component.Airport + "\",";
                body += "\"ParentPaxId\":\"" + component.ParentPaxId + "\",";

                if (!String.IsNullOrEmpty(component.ServiceParent.ToString()) && component.ServiceParent > 0)
                {
                    body += "\"Services\":";
                    body += "{";
                    body += "\"id\":" + component.ServiceParent + "";
                    body += "},";
                }

                if (String.IsNullOrEmpty(component.CobroParticipacionNj))
                {
                    body += "\"CobroParticipacionNj\":null,";
                }
                else
                {
                    body += "\"CobroParticipacionNj\":\"" + component.CobroParticipacionNj + "\",";
                }
                if (String.IsNullOrEmpty(component.Categories))
                {
                    body += "\"Categories\":null,";
                }
                else
                {
                    body += "\"Categories\":\"" + component.Categories + "\",";
                }

                if (String.IsNullOrEmpty(component.ClasificacionPagos))
                {
                    body += "\"ClasificacionPagos\":null,";
                }
                else
                {
                    body += "\"ClasificacionPagos\":\"" + component.ClasificacionPagos + "\",";
                }
                body += "\"Componente\":\"" + component.Componente + "\",";

                if (String.IsNullOrEmpty(component.Costo))
                {
                    body += "\"Costo\":null,";
                }
                else
                {
                    body += "\"Costo\":\"" + component.Costo + "\",";
                }
                body += "\"Incident\":";
                body += "{";
                body += "\"id\":" + Convert.ToInt32(component.Incident) + "";
                body += "},";
                body += "\"Informativo\":\"" + component.Informativo + "\"," +
                 "\"ItemDescription\":\"" + component.ItemDescription.Trim() + "\"," +
                 "\"ItemNumber\":\"" + component.ItemNumber.Trim() + "\",";
                if (component.Itinerary != 0)
                {
                    body += "\"Itinerary\":";
                    body += "{";
                    body += "\"id\":" + component.Itinerary + "";
                    body += "},";
                }
                body += "\"ManualCreated\":\"" + component.MCreated + "\",";
                if (String.IsNullOrEmpty(component.Pagos))
                {
                    body += "\"Pagos\":null,";
                }
                else
                {
                    body += "\"Pagos\":\"" + component.Pagos + "\",";
                }
                body += "\"Paquete\":\"" + component.Paquete + "\",";
                if (String.IsNullOrEmpty(component.ParticipacionCobro))
                {
                    body += "\"ParticipacionCobro\":null,";
                }
                else
                {
                    body += "\"ParticipacionCobro\":\"" + component.ParticipacionCobro + "\",";
                }
                if (String.IsNullOrEmpty(component.FuelId.ToString()))
                {
                    body += "\"fuel_id\":null,";
                }
                else
                {
                    body += "\"fuel_id\":" + component.FuelId + ",";
                }
                if (String.IsNullOrEmpty(component.Precio))
                {
                    body += "\"Precio\":null";
                }
                else
                {
                    body += "\"Precio\":\"" + component.Precio + "\"";
                }
                body += "}";
                GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "POST");
                request.AddHeader("OSvC-CREST-Application-Context", "Create Service");
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    BabyPackages++;
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                    insert = rootObject.id;
                }
                else
                {
                    MessageBox.Show("Componente No creado:" + content);
                }
                return insert;
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error en creación de child: " + ex.Message + "Det" + ex.StackTrace);
                return 0;
            }
        }
        public void InsertComponent(ComponentChild component)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                string body = "{";
                body += "\"Airport\":\"" + component.Airport + "\",";
                body += "\"ParentPaxId\":\"" + component.ParentPaxId + "\",";

                if (!String.IsNullOrEmpty(component.ServiceParent.ToString()) && component.ServiceParent > 0)
                {
                    body += "\"Services\":";
                    body += "{";
                    body += "\"id\":" + component.ServiceParent + "";
                    body += "},";
                }

                if (String.IsNullOrEmpty(component.CobroParticipacionNj))
                {
                    body += "\"CobroParticipacionNj\":null,";
                }
                else
                {
                    body += "\"CobroParticipacionNj\":\"" + component.CobroParticipacionNj + "\",";
                }
                if (String.IsNullOrEmpty(component.Categories))
                {
                    body += "\"Categories\":null,";
                }
                else
                {
                    body += "\"Categories\":\"" + component.Categories + "\",";
                }

                if (String.IsNullOrEmpty(component.ClasificacionPagos))
                {
                    body += "\"ClasificacionPagos\":null,";
                }
                else
                {
                    body += "\"ClasificacionPagos\":\"" + component.ClasificacionPagos + "\",";
                }
                body += "\"Componente\":\"" + component.Componente + "\",";

                if (String.IsNullOrEmpty(component.Costo))
                {
                    body += "\"Costo\":null,";
                }
                else
                {
                    body += "\"Costo\":\"" + component.Costo + "\",";
                }
                body += "\"Incident\":";
                body += "{";
                body += "\"id\":" + Convert.ToInt32(component.Incident) + "";
                body += "},";
                body += "\"Informativo\":\"" + component.Informativo + "\"," +
                 "\"ItemDescription\":\"" + component.ItemDescription + "\"," +
                 "\"ItemNumber\":\"" + component.ItemNumber + "\",";
                if (component.Itinerary != 0)
                {
                    body += "\"Itinerary\":";
                    body += "{";
                    body += "\"id\":" + component.Itinerary + "";
                    body += "},";
                }
                body += "\"ManualCreated\":\"" + component.MCreated + "\",";
                if (String.IsNullOrEmpty(component.Pagos))
                {
                    body += "\"Pagos\":null,";
                }
                else
                {
                    body += "\"Pagos\":\"" + component.Pagos + "\",";
                }
                body += "\"Paquete\":\"" + component.Paquete + "\",";
                if (String.IsNullOrEmpty(component.ParticipacionCobro))
                {
                    body += "\"ParticipacionCobro\":null,";
                }
                else
                {
                    body += "\"ParticipacionCobro\":\"" + component.ParticipacionCobro + "\",";
                }
                if (String.IsNullOrEmpty(component.FuelId.ToString()))
                {
                    body += "\"fuel_id\":null,";
                }
                else
                {
                    body += "\"fuel_id\":" + component.FuelId + ",";
                }
                if (String.IsNullOrEmpty(component.Precio))
                {
                    body += "\"Precio\":null";
                }
                else
                {
                    body += "\"Precio\":\"" + component.Precio + "\"";
                }
                body += "}";
                //GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "POST");
                request.AddHeader("OSvC-CREST-Application-Context", "Create Service");
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    BabyPackages++;
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                    if (component.Paquete == "1")
                    {
                        Packages++;
                        component.ID = rootObject.id;
                        GetComponents(component);
                    }
                }
                else
                {

                    MessageBox.Show("Componente No creado:" + content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en creación de child: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        public void InsertPayable(Services service)
        {
            double TicketAmount = Math.Round(Convert.ToDouble(service.Quantity) * Convert.ToDouble(service.UnitCost), 2);
            try
            {
                BabyPayables++;
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Payables/", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                string body = "{";
                body += "\"Supplier\":\"" + Supplier + "\",";
                body += "\"UOM\":\"" + OUM + "\",";
                body += "\"ItemNumber\":\"" + service.ItemNumber + "\",";
                body += "\"ItemDescription\":\"" + service.Description + "\",";
                body += "\"UnitCost\":\"" + service.UnitCost + "\",";
                body += "\"Quantity\":\"" + service.Quantity + "\",";
                body += "\"TicketAmount\":\"" + TicketAmount + "\",";
                body += "\"Currency\":";
                body += "{";
                body += "\"id\":" + (Currency == "MXN" ? 2 : 1).ToString() + "";
                body += "},";
                body += "\"Services\":";
                body += "{";
                body += "\"id\":" + service.ID + "";
                body += "}";
                body += "}";
                //GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "POST");
                request.AddHeader("OSvC-CREST-Application-Context", "Create Payable");
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (response.StatusCode == HttpStatusCode.Created)
                {

                }
                else
                {
                    MessageBox.Show("Payable No Creado" + content);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en creación de child: " + ex.Message + "Det" + ex.StackTrace);
            }

        }
        public bool GetItineraryCountries(int Itineray)
        {
            try
            {
                bool res = true;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ToAirport.Country.LookupName,FromAirport.Country.LookupName FROM CO.Itinerary WHERE ID  = " + Itineray + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        if (substrings[0] != "MX" || substrings[1] != "MX")
                        {
                            res = false;
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return true;
            }
        }
        public string[] GetCountryLookItinerary(int itinerary)
        {
            try
            {
                string[] res = new string[2];
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT FromAirport.Country.LookupName,ToAirport.Country.LookupName FROM CO.Itinerary WHERE ID =" + itinerary;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        substrings[0] = substrings[0] == "MX" ? "DOMESTIC" : "INTERNATIONAL";
                        substrings[1] = substrings[1] == "MX" ? "DOMESTIC" : "INTERNATIONAL";
                        res = substrings;
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return null;
            }
        }
        private string GetMainHour(String ata, String atd)
        {
            try
            {
                DateTime ATAGH = DateTime.Parse(ata);
                DateTime ATDGH = DateTime.Parse(atd);

                string hour = "EXTRAORDINARIO";
                string hourata = "EXTRAORDINARIO";
                string houratd = "EXTRAORDINARIO";

                //MessageBox.Show("ATAGH: " + ATAGH.ToString());
                //MessageBox.Show("ATDGH: " + ATDGH.ToString());

                if (WHoursList.Count > 0)
                {
                    foreach (WHours w in WHoursList)
                    {
                        if (IsBetween(ATAGH, w.ATAOpens, w.ATACloses) && w.Type == "CRITICO")
                        {
                            hourata = "CRITICO";
                        }
                        if (IsBetween(ATAGH, w.ATAOpens, w.ATACloses) && w.Type == "NORMAL")
                        {
                            hourata = "NORMAL";
                        }
                        if (IsBetween(ATDGH, w.ATDOpens, w.ATDCloses) && w.Type == "CRITICO")
                        {
                            houratd = "CRITICO";
                        }
                        if (IsBetween(ATDGH, w.ATDOpens, w.ATDCloses) && w.Type == "NORMAL")
                        {
                            houratd = "NORMAL";
                        }
                        if (hourata == houratd)
                        {
                            hour = hourata;
                        }
                        else if (hourata == "EXTRAORDINARIO" || houratd == "EXTRAORDINARIO")
                        {
                            hour = "EXTRAORDINARIO";
                        }
                        else if (hourata == "CRITICO" || houratd == "CRITICO")
                        {
                            hour = "CRITICO";
                        }
                        else
                        {
                            hour = "NORMAL";
                        }
                    }
                }
                return hour;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMainHour: " + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private void getArrivalHours(int Arrival, string ATADay, string ATDDay)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT OpensZULUTime,ClosesZULUTime,Type, ID FROM CO.Airport_WorkingHours WHERE Airports =" + Arrival + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                WHoursList = new List<WHours>();
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        WHours hours = new WHours();
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        hours.ATAOpens = DateTime.Parse(ATADay + " " + substrings[0]);
                        hours.ATACloses = DateTime.Parse(ATADay + " " + substrings[1]);
                        hours.ATDOpens = DateTime.Parse(ATDDay + " " + substrings[0]);
                        hours.ATDCloses = DateTime.Parse(ATDDay + " " + substrings[1]);
                        hours.id = Convert.ToInt32(substrings[3].Trim());
                        /*
                        MessageBox.Show("ATA OPEN:" + hours.ATAOpens.ToString() + "\n" +
                            "ATA CLOSE:" + hours.ATACloses.ToString() + "\n" +
                            "ID:" + hours.id.ToString() + "\n" +
                            "ATD OPEN:" + hours.ATDOpens.ToString() + "\n" +
                            "ATD CLOSE:" + hours.ATDCloses.ToString() + "\n" +
                            "ID:" + hours.id.ToString());
                            */
                        if (DateTime.Compare(hours.ATAOpens, hours.ATACloses) > 0)
                        {
                            hours.ATACloses = hours.ATACloses.AddDays(1);
                            hours.ATDCloses = hours.ATDCloses.AddDays(1);
                            //MessageBox.Show(hours.Closes.ToString());
                            /*
                            MessageBox.Show("ATA OPEN:" + hours.ATAOpens.ToString() + "\n" +
                            "ATA CLOSE:" + hours.ATACloses.ToString() + "\n" +
                            "ID:" + hours.id.ToString() + "\n" +
                            "ATD OPEN:" + hours.ATDOpens.ToString() + "\n" +
                            "ATD CLOSE:" + hours.ATDCloses.ToString() + "\n" +
                            "ID:" + hours.id.ToString());
                            */
                        }
                        switch (substrings[2].Trim())
                        {
                            case "1":
                                hours.Type = "EXTRAORDINARIO";
                                break;
                            case "2":
                                hours.Type = "CRITICO";
                                break;
                            case "25":
                                hours.Type = "NORMAL";
                                break;
                        }
                        WHoursList.Add(hours);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("getArrivalHours" + ex.Message + "DEtalle: " + ex.StackTrace);

            }
        }
        private void GetItineraryHours(int Itinerary)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA,ATATime, ATD, ATDTime,ArrivalAirport FROM CO.Itinerary WHERE Incident1 =" + IncidentID + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        ATA = DateTime.Parse(substrings[0] + " " + substrings[1]);
                        ATD = DateTime.Parse(substrings[2] + " " + substrings[3]);
                        getArrivalHours(int.Parse(substrings[4]), ATA.ToString("yyyy-MM-dd"), ATD.ToString("yyyy-MM-dd"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetItineraryHours" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }
        public void UpdatePackageCost()
        {
            try
            {

                PaquetesCostos = GetAllPaquetesCostos();

                double antelacion = 0;
                double extension = 0;
                double minover = 0;

                List<Services> services = new List<Services>();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Services,Itinerary,ItemNumber,Airport,ItemDescription,Services.ItemNumber  FROM CO.Services  WHERE Incident =" + IncidentID + "  AND Paquete = '0' AND (Componente = '1' AND Broken = 0) Order BY Services.CreatedTime ASC";
                if (CustomerName.Contains("NETJETS"))
                {
                    queryString = "SELECT ID,Services,Itinerary,ItemNumber,Airport,ItemDescription,Services.ItemNumber FROM CO.Services WHERE Incident =" + IncidentID + " AND(Paquete = '0' OR Componente = '0') AND Services IS NOT NULL AND Broken = 0 ORDER BY Services.CreatedTime ASC";
                }
                GlobalContext.LogMessage(queryString.ToString());
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 500, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Services service = new Services();
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        service.ID = substrings[0];
                        service.ParentPax = substrings[1];
                        service.Itinerary = substrings[2];
                        // IdItinerary = substrings[2];
                        service.ItemNumber = substrings[3];
                        service.Airport = substrings[4];
                        service.Description = substrings[5];
                        service.ItemDescPadre = substrings[6];
                        services.Add(service);
                    }
                }
                if (services.Count > 0)
                {
                    // MessageBox.Show("Itinerary ID: " + IdItinerary.ToString());
                    // ATA = getATAItinerary(Convert.ToInt32(IdItinerary));
                    // string whType = getWH();
                    // GetItineraryHours(int.Parse(IdItinerary),whType);
                    foreach (Services item in services)
                    {
                        SearchPayable(item);
                        if (item.ItemDescPadre == "LOGIROT0063")
                        {
                            continue;
                        }

                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        item.Cost = GetCostoPaquete(item, getItemNumber(Convert.ToInt32(item.ParentPax)), claseCliente).ToString();

                        if (item.Cost == "0")
                        {
                            continue;
                        }
                        item.UnitCost = item.Cost;
                        item.Quantity = "1";
                        minover = 0;


                        if (!AirportOpen24(Convert.ToInt32(item.Itinerary)) && (OUM == "MIN" || OUM == "HHR" || OUM == "HR"))
                        {
                            int arrival = getArrivalAirport(Convert.ToInt32(item.Itinerary));
                            if (arrival != 0)
                            {
                                DateTime openDate;
                                DateTime closeDate;
                                string open = getOpenArrivalAirport(arrival);
                                string close = getCloseArrivalAirport(arrival);
                                DateTime ATA = getATAItinerary(Convert.ToInt32(item.Itinerary));
                                DateTime ATD = getATDItinerary(Convert.ToInt32(item.Itinerary));
                                openDate = DateTime.Parse(ATA.Date.ToShortDateString() + " " + open);
                                closeDate = DateTime.Parse(ATA.Date.ToShortDateString() + " " + close);
                                if (IsBetween(ATA, openDate, closeDate))
                                {
                                    antelacion = (ATA - openDate).TotalMinutes;
                                }
                                extension = ((ATD - openDate).TotalMinutes) + 15;
                                if (ATA.Date != ATD.Date)
                                {
                                    openDate = DateTime.Parse(ATD.Date.ToShortDateString() + " " + open);
                                    closeDate = DateTime.Parse(ATD.Date.ToShortDateString() + " " + close);
                                    if (IsBetween(ATD, openDate, closeDate))
                                    {
                                        extension = ((ATD - openDate).TotalMinutes) + 15;
                                    }
                                    else
                                    {
                                        extension = 0;
                                    }
                                }
                                if (extension > 0)
                                {
                                    minover = extension < 0 ? 0 : extension;
                                }
                                if (ATA.Date != ATD.Date)
                                {
                                    minover = (antelacion < 0 ? 0 : antelacion) + (extension < 0 ? 0 : extension);
                                }
                            }
                        }

                        switch (OUM)
                        {
                            case "TW":
                                double b;
                                if (double.TryParse(item.Cost, out b))
                                {
                                    item.Quantity = GetMTOW(ICAOId);
                                    item.Cost = GetMTOWPrice(item.Cost);
                                }
                                break;
                            case "HHR":
                                double c;
                                if (double.TryParse(item.Cost, out c))
                                {
                                    // if (minover > 0 ) && item.ItemNumber != "PFEESAF0009")
                                    // {
                                    // TimeSpan t = TimeSpan.FromMinutes(minover);
                                    // item.Quantity = (Math.Ceiling(t.TotalMinutes / 30)).ToString();
                                    
                                    if (item.ItemNumber == "PFEESAF0009")
                                    {
                                        item.Quantity = "4";
                                        double tw = Convert.ToDouble(String.IsNullOrEmpty(GetMTOW(ICAOId)) ? "0" : GetMTOW(ICAOId));
                                        item.Cost = (Convert.ToDouble(tw * Convert.ToDouble(item.Cost)) * 4).ToString();
                                        // MessageBox.Show("Cost: " + item.Cost);
                                    }
                                    else
                                    {
                                        item.Quantity = (Math.Ceiling(GetMinutesLeg() / 30)).ToString();
                                        item.Cost = (Convert.ToDouble(item.Cost) * Convert.ToDouble(item.Quantity)).ToString();
                                    }
                                }
                                /*
                                else if (item.ItemNumber == "PFEESAF0009")
                                {
                                    item.Quantity = "4";
                                    double tw = Convert.ToDouble(String.IsNullOrEmpty(GetMTOW(ICAOId)) ? "0" : GetMTOW(ICAOId));
                                    item.Cost = (Convert.ToDouble(tw * Convert.ToDouble(item.Cost)) * 4).ToString();
                                    // MessageBox.Show("Cost: " + item.Cost);
                                }
                                */
                                else
                                {
                                    item.Cost = "0";
                                }
                                //}
                                break;
                            case "HR":
                                double d;
                                if (double.TryParse(item.Cost, out d))
                                {
                                    // if (minover > 0) && item.ItemNumber != "PFEESAF0009")
                                    // {
                                    // TimeSpan t = TimeSpan.FromMinutes(minover);
                                    if (item.ItemNumber == "PFEESAF0009")
                                    {
                                        item.Quantity = "2";
                                        double tw = Convert.ToDouble(String.IsNullOrEmpty(GetMTOW(ICAOId)) ? "0" : GetMTOW(ICAOId));
                                        item.Cost = (Convert.ToDouble(tw * Convert.ToDouble(item.Cost)) * 2).ToString();
                                        // MessageBox.Show("Cost: " + item.Cost);
                                    }
                                    else
                                    {
                                        item.Quantity = (Math.Ceiling(GetMinutesLeg() / 60)).ToString();
                                        item.Cost = (Convert.ToDouble(item.Cost) * Convert.ToDouble(item.Quantity)).ToString();
                                    }
                                }
                                /*
                                else if (item.ItemNumber == "PFEESAF0009")
                                {
                                    item.Quantity = "2";
                                    double tw = Convert.ToDouble(String.IsNullOrEmpty(GetMTOW(ICAOId)) ? "0" : GetMTOW(ICAOId));
                                    item.Cost = (Convert.ToDouble(tw * Convert.ToDouble(item.Cost)) * 2).ToString();
                                    // MessageBox.Show("Cost: " + item.Cost);
                                }
                                */
                                else
                                {
                                    item.Cost = "0";
                                    // }
                                }
                                break;
                            case "MIN":
                                double e;
                                if (double.TryParse(item.Cost, out e))
                                {
                                    // if (minover > 0)
                                    // {
                                    // TimeSpan t = TimeSpan.FromMinutes(minover);
                                    item.Quantity = Math.Ceiling(GetMinutesLeg()).ToString();
                                    item.Cost = (Convert.ToDouble(item.Cost) * Convert.ToDouble(item.Quantity)).ToString();
                                }
                                else
                                {
                                    item.Cost = "0";
                                    // }
                                }
                                break;
                        }
                        if (getServiceParentName(item.ID) == "IPFERPS0052")
                        {
                            if (item.ItemNumber == "TUAASER240" || item.ItemNumber == "DSMSYSW0117")
                            {
                                item.Quantity = getPaxOutBound(item.ParentPax);
                            }
                            if (item.ItemNumber == "DNIDIPS0187")
                            {
                                item.Quantity = getPaxInBound(item.ParentPax);
                            }
                        }
                        if (double.Parse(item.Cost) > 0)
                        {
                            InsertPayable(item);
                        }
                        var tot = watch.Elapsed;
                        GlobalContext.LogMessage("GetCostoPaquete: " + tot.TotalSeconds.ToString() + "def: " + item.ItemNumber);
                        /*
                        double price = 0;
                        double priceP = 0;
                        double PriceCh = 0;
                        if (!String.IsNullOrEmpty(item.ParentPax))
                        {
                            priceP = getPaxPrice(item.ParentPax);
                            PriceCh = getPaxPrice(item.ID);
                            price = PriceCh + priceP;
                            UpdatePaxPrice(item.ID, PriceCh);
                            UpdatePaxPrice(item.ParentPax, price);
                        }
                        else
                        {
                            price = getPaxPrice(item.ID);
                            UpdatePaxPrice(item.ID, price);
                        }
                        */
                    }
                }
                if (minover != 0)
                {
                    if (antelacion > 0 && extension == 0)
                    {
                        MessageBox.Show("OVERTIME ARRIVAL: " + antelacion + " minutes.");
                    }
                    else if (extension > 0 && antelacion == 0)
                    {
                        MessageBox.Show("OVERTIME DEPARTURE: " + extension + " minutes.");
                    }
                    else
                    {
                        MessageBox.Show("OVERTIME ARRIVAL & DEPARTURE: " + minover + " minutes.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdatePackageCost:+ " + ex.Message + " Det: " + ex.StackTrace);
                GlobalContext.LogMessage("UpdatePackageCost: " + ex.Message + " Det: " + ex.StackTrace);
            }
        }
        public static bool IsBetween(DateTime input, DateTime date1, DateTime date2)
        {
            return (input > date1 && input < date2);
        }
        public int getArrivalAirport(int Itinerarie)
        {
            int arriv = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ArrivalAirport FROM Co.Itinerary  WHERE ID =" + Itinerarie;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    arriv = String.IsNullOrEmpty(data) ? 0 : Convert.ToInt32(data);
                }
            }
            return arriv;
        }
        public string getOpenArrivalAirport(int Arrival)
        {
            string opens = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT OpensZuluTime FROM Co.Airport_WorkingHours  WHERE Airports =" + Arrival + " AND Type = 1";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    opens = data;
                }
            }
            return opens;
        }
        public DateTime getATAItinerary(int Itinerarie)
        {
            try
            {
                string ATA = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA,ATATime FROM Co.Itinerary WHERE ID = " + Itinerarie;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        ATA = substrings[0] + " " + substrings[1];
                    }
                }
                return DateTime.Parse(ATA);
            }
            catch (Exception ex)
            {
                MessageBox.Show("getATAItinerary: " + ex.Message + "Detail: " + ex.StackTrace);
                return DateTime.Now;
            }
        }
        public double GetMinutesLeg()
        {
            try
            {
                double minutes = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                //String queryString = "SELECT (Date_Diff(ATA_ZUTC,ATD_ZUTC)/60) FROM CO.Itinerary WHERE ID =" + Itinerarie + "";
                String queryString = "SELECT ATA,ATATime,ATD,ATDTime FROM CO.Itinerary WHERE ID = " + IdItinerary.ToString();
                GlobalContext.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        DateTime ATA = DateTime.Parse(substrings[0] + " " + substrings[1]);
                        DateTime ATD = DateTime.Parse(substrings[2] + " " + substrings[3]);
                        minutes = (ATD - ATA).TotalMinutes;
                    }
                }
                if (claseCliente == "ASI_SECURITY")
                {
                    minutes = minutes - 120;
                }
                TimeSpan t = TimeSpan.FromMinutes(minutes);
                return Math.Ceiling(t.TotalMinutes);
            }
            catch (Exception ex)
            {
                GlobalContext.LogMessage("GetMinutesLeg: " + ex.Message + "Det: " + ex.StackTrace);
                return 0;
            }
        }
        public DateTime getATDItinerary(int Itinerarie)
        {
            try
            {
                string ATD = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATD,ATDTime FROM Co.Itinerary WHERE ID = " + Itinerarie;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        ATD = substrings[0] + " " + substrings[1];
                    }
                }

                return DateTime.Parse(ATD);
            }
            catch (Exception ex)
            {
                MessageBox.Show("getATAItinerary: " + ex.Message + "Detail: " + ex.StackTrace);
                return DateTime.Now;
            }
        }
        public string getCloseArrivalAirport(int Arrival)
        {
            string closes = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ClosesZuluTime  FROM Co.Airport_WorkingHours  WHERE Airports =" + Arrival + " AND Type = 1";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    closes = data;
                }
            }
            return closes;
        }
        public string GetMTOWPrice(string cost)
        {
            try
            {
                double mtow = Convert.ToDouble(GetMTOW(ICAOId));
                double costMTOW = Convert.ToDouble(cost);
                double price = (mtow * costMTOW);
                return Math.Round((price), 4).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMTOWPrice: " + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        public bool AirportOpen24(int Itinerarie)
        {
            try
            {
                bool open = true;

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.HoursOpen24 FROM Co.Itinerary  WHERE ID =" + Itinerarie;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        open = data == "1" ? true : false;
                    }
                }

                return open;
            }
            catch (Exception ex)
            {
                MessageBox.Show("AirportOpen24: " + ex.Message + "Detail: " + ex.StackTrace);
                return false;
            }
        }
        private string getPaxOutBound(string Service)
        {
            try
            {
                string PaxOut = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT PaxOutBound FROM CO.Services WHERE ID = " + Service;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        PaxOut = data;
                    }
                }
                return String.IsNullOrEmpty(PaxOut) ? "1" : PaxOut;

            }
            catch (Exception ex)
            {
                MessageBox.Show("getPaxOutBound" + ex.Message + "Det:" + ex.StackTrace);
                return "1";
            }
        }
        private string getPaxInBound(string Service)
        {
            try
            {
                string PaxInBound = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT PaxInBound FROM CO.Services WHERE ID = " + Service;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        PaxInBound = data;
                    }
                }
                return String.IsNullOrEmpty(PaxInBound) ? "1" : PaxInBound;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getPaxInBound" + ex.Message + "Det:" + ex.StackTrace);
                return "1";
            }
        }
        private string getServiceParentName(string ID)
        {
            try
            {
                string ServiceParentName = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Services.ItemNumber FROM CO.Services WHERE ID = " + ID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        ServiceParentName = data;
                    }
                }
                return String.IsNullOrEmpty(ServiceParentName) ? "" : ServiceParentName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getServiceParentName" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private string GetMTOW(string idICAO)
        {
            try
            {
                string weight = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Weight FROM CO.AircraftType WHERE ICAODesignator= '" + idICAO + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        weight = data;
                    }
                }
                return String.IsNullOrEmpty(weight) ? "" : weight;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMTOW" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        public void UpdatePaxPrice(string id, double price)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                // Información de precios costos
                body +=
                    "\"Costo\":\"" + price + "\"";

                body += "}";
                GlobalContext.LogMessage("Actualza desde Child:" + body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {id}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdatePaxPrice: " + ex.Message + " Det: " + ex.StackTrace);
            }

        }
        public void InformativeNJ(string id)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                body +=
                    "\"Informativo\":\"1\"";
                body += "}";
                //GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {" + id + "}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " InformativeNJ: " + ex.StackTrace);
            }
        }
        public void BrokenPackage(string id)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                body +=
                    "\"Broken\":true";
                body += "}";
                //GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {" + id + "}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " BrokenPackage: " + ex.StackTrace);
            }
        }
        public void SearchPayable(Services service)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + service.ID + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                body +=
                    "\"Broken\":true";
                body += "}";
                //GlobalContext.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {" + service.ID + "}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " InformativeNJ: " + ex.StackTrace);
            }
        }
        private ClaseParaPaquetes.RootObject GetAllPaquetesCostos()
        {
            try
            {
                ClaseParaPaquetes.RootObject rootObjectCat = new ClaseParaPaquetes.RootObject();
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", pswCPQ);
                string definicion = "?totalResults=true&q={str_ft_arrival:'" + arridepart[0].ToString() + "'" +
                                   ",str_ft_depart:'" + arridepart[1].ToString() + "'" +
                                   ",str_schedule_type:'" + main + "'" +
                                   ",bol_int_fbo:'" + fbo + "'" +
                                   ",$and:[{$or:[{str_icao_iata_code:'IO_AEREO_" + airport + "'},{str_icao_iata_code:{$exists:false}}]}," +
                                   "{$or:[{str_aircraft_type:'" + ICAOId + "'},{str_aircraft_type:{$exists:false}}]}]}";
                var request = new RestRequest("rest/v6/customCostosPaquetes/" + definicion, Method.GET);
                GlobalContext.LogMessage("AllCostosPaquetes: " + definicion);
                IRestResponse response = client.Execute(request);
                rootObjectCat = JsonConvert.DeserializeObject<ClaseParaPaquetes.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    return rootObjectCat;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                GlobalContext.LogMessage("GetAllPaquetesCostos: " + ex.Message + "Det" + ex.StackTrace);
                return null;
            }
        }

        private double GetCostoPaquete(Services service, string parentNumber, string clase)
        {
            try
            {
                double amount = 0;

                if (PaquetesCostos == null)
                {
                    var client = new RestClient("https://iccs.bigmachines.com/");
                    string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                    string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                    client.Authenticator = new HttpBasicAuthenticator("servicios", pswCPQ);

                    string definicion = "?totalResults=true&q={str_item_number:'" + service.ItemNumber + "'," +
                                    "str_ft_arrival:'" + arridepart[0] + "'" +
                                    ",str_ft_depart:'" + arridepart[1] + "'" +
                                    ",str_schedule_type:'" + main + "'" +
                                    ",bol_int_fbo:'" + fbo + "'" +
                                    ",$and:[{$or:[{str_icao_iata_code:'IO_AEREO_" + service.Airport + "'},{str_icao_iata_code:{$exists:false}}]}," +
                                    "{$or:[{str_aircraft_type:'" + ICAOId + "'},{str_aircraft_type:{$exists:false}}]}]}";
                    GlobalContext.LogMessage(definicion);
                    var request = new RestRequest("rest/v6/customCostosPaquetes/" + definicion, Method.GET);
                    IRestResponse response = client.Execute(request);
                    ClaseParaPaquetes.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaPaquetes.RootObject>(response.Content);
                    if (rootObjectCat.items.Count > 0)
                    {
                        amount = rootObjectCat.items[0].flo_cost;
                        Currency = rootObjectCat.items[0].str_currency_code;
                        Supplier = string.IsNullOrEmpty(rootObjectCat.items[0].str_vendor_name) ? "NO SUPPLIER" : rootObjectCat.items[0].str_vendor_name;
                        OUM = rootObjectCat.items[0].str_uom_code;
                    }
                    else
                    {
                        amount = 0;
                    }
                }
                else
                {
                    var filtro = PaquetesCostos.items.Where(l => l.str_item_number == service.ItemNumber).ToList();
                    GlobalContext.LogMessage(filtro.Count().ToString() + " :" + service.ItemNumber);

                    foreach (ClaseParaPaquetes.Item rootObjectCat in filtro)
                    {
                        amount = rootObjectCat.flo_cost;
                        Currency = rootObjectCat.str_currency_code;
                        Supplier = string.IsNullOrEmpty(rootObjectCat.str_vendor_name) ? "NO SUPPLIER" : rootObjectCat.str_vendor_name;
                        OUM = rootObjectCat.str_uom_code;
                        if (rootObjectCat.str_client_category == clase)
                        {
                            continue;
                        }
                    }
                }
                return amount;
            }
            catch (Exception ex)
            {
                GlobalContext.LogMessage("GetCosto: " + ex.Message + "Det: " + ex.StackTrace);
                return 0;
            }
        }
        public double getPaxPrice(string PaxId)
        {
            double price = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT SUM(TicketAmount) FROM CO.Payables WHERE Services.Incident =" + IncidentID + "  AND Services.Services = " + PaxId + " GROUP BY Services.Services";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    price = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                }
            }
            return price;
        }
        public string getPassword(string application)
        {
            string password = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Password FROM CO.Password WHERE Aplicacion='" + application + "'";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    password = String.IsNullOrEmpty(data) ? "" : data;
                }
            }
            return password;
        }
        public string GetClase()
        {
            try
            {
                string clase = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.C.clase FROM Incident WHERE ID  = " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        clase = String.IsNullOrEmpty(data) ? "TODOS" : data;
                        clase = clase == "G&C" ? "G%C" : clase;
                    }
                }

                return clase;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return "";
            }
        }
        public string getItemNumber(int Service)
        {
            try
            {
                string iNumber = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ItemNumber FROM CO.Services WHERE ID  = " + Service;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        iNumber = data;
                    }
                }
                return iNumber;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return "";
            }
        }
        public int GetFBOValue(string Itinerary)
        {
            try
            {
                int fbo = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT SalesMethod.name FROM CO.Itinerary WHERE ID  = " + Itinerary;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        fbo = data == "FBO" ? 1 : 0;
                    }
                }
                return fbo;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return 0;
            }
        }
        public string GetSRType()
        {
            try
            {
                string SRTYPE = "";
                if (IncidentID != 0)
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT I.Customfields.c.sr_type.LookupName FROM Incident I WHERE id=" + IncidentID + "";
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            SRTYPE = data;
                        }
                    }
                }
                switch (SRTYPE)
                {
                    case "Catering":
                        SRTYPE = "CATERING";
                        break;
                    case "FCC":
                        SRTYPE = "FCC";
                        break;
                    case "FBO":
                        SRTYPE = "FBO";
                        break;
                    case "Fuel":
                        SRTYPE = "FUEL";
                        break;
                    case "Hangar Space":
                        SRTYPE = "GYCUSTODIA";
                        break;
                    case "SENEAM Fee":
                        SRTYPE = "SENEAM";
                        break;
                    case "Permits":
                        SRTYPE = "PERMISOS";
                        break;
                }
                return SRTYPE;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetType: " + ex.Message + "Detail: " + ex.StackTrace);
                return "";
            }
        }
    }

    [AddIn("Package Breakdown", Version = "1.0.0.0")]
    public class WorkspaceRibbonButtonFactory : IWorkspaceRibbonButtonFactory
    {
        private IGlobalContext globalContext { get; set; }
        public IWorkspaceRibbonButton CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new WorkspaceRibbonAddIn(inDesignMode, RecordContext, globalContext);
        }
        public System.Drawing.Image Image32
        {
            get { return Properties.Resources.service32; }
        }
        public System.Drawing.Image Image16
        {
            get { return Properties.Resources.service16; }
        }
        public string Text
        {
            get { return "Package Breakdown"; }
        }
        public string Tooltip
        {
            get { return "Create Services From Packages"; }
        }
        public bool Initialize(IGlobalContext GlobalContext)
        {
            this.globalContext = GlobalContext;
            return true;
        }
    }

}