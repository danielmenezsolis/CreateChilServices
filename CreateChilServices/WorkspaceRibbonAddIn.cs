using System;
using System.AddIn;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
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
        public string InformativoPadre { get; set; }
        public string ATD { get; set; }
        public string ATA { get; set; }
        public List<WHours> WHoursList { get; set; }
        public string SRType { get; set; }
        public string Currency { get; set; }
        public string Supplier { get; set; }
        public string OUM { get; set; }
        public string ICAOId { get; set; }

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
            try
            {
                DialogResult dr = MessageBox.Show("¿Would you like to create child services from Packages?",
                          "Confirm", MessageBoxButtons.YesNo);
                switch (dr)
                {
                    case DialogResult.Yes:
                        BabyPackages = 0;
                        Packages = 0;
                        if (Init())
                        {
                            Incident = (IIncident)RecordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                            IncidentID = Incident.ID;
                            ICAOId = getICAODesi(IncidentID);
                            SRType = GetSRType();
                            GetDeleteComponents();
                            CreateChildComponents();
                            MessageBox.Show("Packages Found: " + Packages + " ChildServices Created: " + BabyPackages);
                            UpdatePackageCost();
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en Click: " + ex.Message + "Det" + ex.StackTrace);
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

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID FROM CO.Services WHERE Componente = '1' AND Incident = " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            DeleteServices(Convert.ToInt32(data));
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void DeleteServices(int id)
        {
            try
            {

                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id, Method.DELETE)
                {
                    RequestFormat = DataFormat.Json
                };
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "DELETE");
                request.AddHeader("OSvC-CREST-Application-Context", "Delete Service");

                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (String.IsNullOrEmpty(content))
                {
                    //MessageBox.Show(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DElete: " + ex.InnerException.ToString());
            }
        }
        public void CreateChildComponents()
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Airport,ItemNumber,Itinerary,Informativo FROM CO.Services WHERE Paquete = '1' AND COMPONENTE IS NULL AND  Incident =  " + IncidentID;
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
                            component.Itinerary = Convert.ToInt32(substrings[3]);
                            InformativoPadre = substrings[4];
                            GetComponents(component);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                GlobalContext.LogMessage(envelope);
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
                MessageBox.Show(e.Message);
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
                MessageBox.Show(ex.StackTrace);
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
                GlobalContext.LogMessage(envelope);

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

                if (InformativoPadre == "1")
                {
                    component.Informativo = "1";
                }

                return component;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return null;
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
                    if (component.Paquete == "1")
                    {
                        Packages++;
                        component.ID = rootObject.id;
                        GetComponents(component);
                    }
                }
                else
                {
                    MessageBox.Show(content);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en creación de child: " + ex.Message + "Det" + ex.StackTrace);
            }

        }

        public void InsertPayable(Services service)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Payables/", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                string body = "{";
                body += "\"Supplier\":\"" + Supplier + "\",";
                body += "\"TicketAmount\":\"" + service.Cost + "\",";
                body += "\"Currency\":";
                body += "{";
                body += "\"id\":" + (Currency == "MXN" ? 2 : 1).ToString() + "";
                body += "},";
                body += "\"Services\":";
                body += "{";
                body += "\"id\":" + service.ParentPax + "";
                body += "}";
                body += "}";

                GlobalContext.LogMessage(body);
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
                    MessageBox.Show(content);
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
                String queryString = "SELECT ArrivalAirport.Country.LookupName,ToAirport.LookupName FROM CO.Itinerary WHERE ID =" + itinerary + "";
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
        private string GetMainHour(string ata, string atd)
        {
            try
            {
                DateTime ArriveDate = DateTime.Parse(ata);
                DateTime DeliverDate = DateTime.Parse(atd);
                string hour = "EXTRAORDINARIO";
                if (WHoursList.Count > 0)
                {
                    foreach (WHours w in WHoursList)
                    {
                        double totalminutesOpen = (ArriveDate - w.Opens).TotalMinutes;
                        double totalminutesClose = (w.Closes - DeliverDate).TotalMinutes;
                        if (ArriveDate.CompareTo(w.Opens) >= 0 && ArriveDate.CompareTo(w.Closes) <= 0 && w.Type == "NORMAL" &&
                                                    DeliverDate.CompareTo(w.Opens) >= 0 && DeliverDate.CompareTo(w.Closes) <= 0)
                        {
                            hour = "NORMAL";
                        }
                        else if (ArriveDate.CompareTo(w.Opens) >= 0 && ArriveDate.CompareTo(w.Closes) <= 0 && w.Type == "CRITICO" &&
                            DeliverDate.CompareTo(w.Opens) >= 0 && DeliverDate.CompareTo(w.Closes) <= 0)
                        {
                            hour = "CRITICO";
                        }
                    }
                }
                return hour;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private void getArrivalHours(int Arrival, string Open, string Close)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT OpensZULUTime,ClosesZULUTime,Type FROM CO.Airport_WorkingHours WHERE Airports =" + Arrival + "";
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
                        hours.Opens = DateTime.Parse(Open + " " + substrings[0].Trim());
                        hours.Closes = DateTime.Parse(Close + " " + substrings[1].Trim());
                        switch (substrings[2].Trim())
                        {
                            case "1":
                                hours.Type = "EXTRAORDINARIO";
                                break;
                            case "2":
                                hours.Type = "CRITICO";
                                break
                                    ;
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
                String queryString = "SELECT ATA_ZUTC,ATD_ZUTC,ArrivalAirport FROM CO.Itinerary WHERE Incident1 =" + IncidentID + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        ATA = DateTimeOffset.Parse(substrings[0]).ToString();
                        ATD = DateTimeOffset.Parse(substrings[1]).ToString();
                        getArrivalHours(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]), substrings[0].Substring(0, 10), substrings[1].Substring(0, 10));
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
                List<Services> services = new List<Services>();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Services,Itinerary,ItemNumber,Airport FROM CO.Services  WHERE Incident =" + IncidentID + "  AND Paquete = '0' AND Componente = '1' Order BY Services.CreatedTime ASC";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
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
                        service.ItemNumber = substrings[3];
                        service.Airport = substrings[4];

                        services.Add(service);
                    }
                }
                if (services.Count > 0)
                {
                    string clase = GetClase();
                    foreach (Services item in services)
                    {
                        string[] vuelos = GetCountryLookItinerary(int.Parse(item.Itinerary));
                        GetItineraryHours(int.Parse(item.Itinerary));
                        string MH = GetMainHour(ATA, ATD);
                        item.Cost = GetCostoPaquete(item, getItemNumber(Convert.ToInt32(item.ParentPax)), vuelos, MH, clase).ToString();

                        switch (OUM)
                        {
                            case "TW":
                                double b;
                                if (double.TryParse(item.Cost, out b))
                                {
                                    item.Cost = GetMTOWPrice(item.Cost);
                                }
                                break;
                            case "HHR":
                                double c;
                                if (double.TryParse(item.Cost, out c))
                                {
                                    double hhr = GetMinutesLeg(int.Parse(item.Itinerary), clase) * 2;
                                    item.Cost = (Convert.ToDouble(item.Cost) * hhr).ToString();
                                }
                                break;
                            case "HR":
                                double d;
                                if (double.TryParse(item.Cost, out d))
                                {
                                    double hr = GetMinutesLeg(int.Parse(item.Itinerary), clase);
                                    item.Cost = (Convert.ToDouble(item.Cost) * hr).ToString();
                                }
                                break;
                        }
                        if (double.Parse(item.Cost) > 0)
                        {
                            InsertPayable(item);
                        }


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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " Det: " + ex.StackTrace);
            }

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
        public double GetMinutesLeg(int Itinerary, string clase)
        {
            try
            {
                double minutes = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT (Date_Diff(ATA_ZUTC,ATD_ZUTC)/60) FROM CO.Itinerary WHERE ID =" + Itinerary.ToString() + "";
                GlobalContext.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        minutes = Convert.ToDouble(data);
                    }
                }
                if (clase == "ASI_SECURITY")
                {
                    minutes = minutes - 120;
                }
                TimeSpan t = TimeSpan.FromMinutes(minutes);
                return Math.Ceiling(t.TotalHours);
            }
            catch (Exception ex)
            {
                GlobalContext.LogMessage("GetMinutesLeg: " + ex.Message + "Det: " + ex.StackTrace);
                return 0;
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
                GlobalContext.LogMessage(body);
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
                MessageBox.Show(ex.Message + " Det: " + ex.StackTrace);
            }

        }
        private double GetCostoPaquete(Services service, string parentNumber, string[] vuelos, string main, string clase)
        {
            try
            {
                int fbo = 0;
                if (SRType == "FBO")
                {
                    fbo = 1;
                }
                else if (SRType == "FCC")
                {
                    fbo = GetFBOValue(service.Itinerary);
                }
                double amount = 0;

                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?totalResults=true&q={str_item_number:'" + service.ItemNumber + "',str_icao_iata_code:'IO_AEREO_" + service.Airport + "',str_ft_arrival:'" + vuelos[0] + "',str_ft_depart:'" + vuelos[1] + "',str_schedule_type:'" + main + "',bol_int_fbo:" + fbo + ",$or:[{str_package:'" + parentNumber + "'},{str_package:'TODOS'}],$or:[{str_client_category:{$like:'" + clase + "'}},{str_client_category:{$exists:false}}]}";
                GlobalContext.LogMessage(definicion);
                var request = new RestRequest("rest/v6/customCostosPaquetes/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaPaquetes.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaPaquetes.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    amount = rootObjectCat.items[0].flo_cost;
                    Currency = rootObjectCat.items[0].str_currency_code;
                    Supplier = rootObjectCat.items[0].str_vendor_name;
                    OUM = rootObjectCat.items[0].str_uom_code;
                }
                else
                {
                    amount = 0;
                }

                return amount;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;
            }
        }
        public double getPaxPrice(string PaxId)
        {
            double price = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT SUM(TicketAmount) FROM CO.Payables WHERE Services.Incident =" + IncidentID + "  AND Services.Services = " + PaxId + " GROUP BY Services.Services   ";
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