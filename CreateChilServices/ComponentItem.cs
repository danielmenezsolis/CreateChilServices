using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateChilServices
{
    [XmlRoot(ElementName = "DATA_DS")]
    public class DATA_DS
    {
        [XmlElement(ElementName = "PAEREO")]
        public string PAEREO { get; set; }
        [XmlElement(ElementName = "PITEM")]
        public string PITEM { get; set; }
        [XmlElement(ElementName = "PAQUETE")]
        public PAQUETE PAQUETE { get; set; }
    }

    [XmlRoot(ElementName = "CAT_NIVEL_1")]
    public class CAT_NIVEL_1
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "NIVEL_1")]
    public class NIVEL_1
    {
        [XmlElement(ElementName = "ID_ITEM_HIJO")]
        public string ID_ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_HIJO")]
        public string ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_NUM")]
        public string ITEM_NUM { get; set; }
        [XmlElement(ElementName = "DESCRIPTION_HIJO")]
        public string DESCRIPTION_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV_HIJO")]
        public string XX_PAQUETE_INV_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO_HIJO")]
        public string XX_PARTICIPACION_COBRO_HIJO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ_HIJO")]
        public string XX_COBRO_PARTICIPACION_NJ_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAGOS_HIJO")]
        public string XX_PAGOS_HIJO { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO_HIJO")]
        public string XX_INFORMATIVO_HIJO { get; set; }
        [XmlElement(ElementName = "PAQUETE_2")]
        public PAQUETE_2 PAQUETE_2 { get; set; }
        [XmlElement(ElementName = "CAT_NIVEL_1")]
        public List<CAT_NIVEL_1> CAT_NIVEL_1 { get; set; }
        [XmlElement(ElementName = "XX_CLASIFICACION_PAGO_HIJO")]
        public string XX_CLASIFICACION_PAGO_HIJO { get; set; }
    }

    [XmlRoot(ElementName = "CAT_PAQ")]
    public class CAT_PAQ
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "PAQUETE")]
    public class PAQUETE
    {
        [XmlElement(ElementName = "ORGANIZATION_ID")]
        public string ORGANIZATION_ID { get; set; }
        [XmlElement(ElementName = "NAME")]
        public string NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV")]
        public string XX_PAQUETE_INV { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO")]
        public string XX_PARTICIPACION_COBRO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ")]
        public string XX_COBRO_PARTICIPACION_NJ { get; set; }
        [XmlElement(ElementName = "XX_PAGOS")]
        public string XX_PAGOS { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO")]
        public string XX_INFORMATIVO { get; set; }
        [XmlElement(ElementName = "NIVEL_1")]
        public List<NIVEL_1> NIVEL_1 { get; set; }
        [XmlElement(ElementName = "CAT_PAQ")]
        public CAT_PAQ CAT_PAQ { get; set; }
    }


    [XmlRoot(ElementName = "PAQUETE_2")]
    public class PAQUETE_2
    {
        [XmlElement(ElementName = "ORGANIZATION_ID")]
        public string ORGANIZATION_ID { get; set; }
        [XmlElement(ElementName = "NAME")]
        public string NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV")]
        public string XX_PAQUETE_INV { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO")]
        public string XX_PARTICIPACION_COBRO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ")]
        public string XX_COBRO_PARTICIPACION_NJ { get; set; }
        [XmlElement(ElementName = "XX_PAGOS")]
        public string XX_PAGOS { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO")]
        public string XX_INFORMATIVO { get; set; }
        [XmlElement(ElementName = "NIVEL_2")]
        public List<NIVEL_2> NIVEL_2 { get; set; }
        [XmlElement(ElementName = "CAT_PAQ_2")]
        public CAT_PAQ_2 CAT_PAQ_2 { get; set; }
    }

    [XmlRoot(ElementName = "CAT_PAQ_2")]
    public class CAT_PAQ_2
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "CAT_NIVEL_2")]
    public class CAT_NIVEL_2
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "NIVEL_2")]
    public class NIVEL_2
    {
        [XmlElement(ElementName = "ID_ITEM_HIJO")]
        public string ID_ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_HIJO")]
        public string ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_NUM")]
        public string ITEM_NUM { get; set; }
        [XmlElement(ElementName = "DESCRIPTION_HIJO")]
        public string DESCRIPTION_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV_HIJO")]
        public string XX_PAQUETE_INV_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO_HIJO")]
        public string XX_PARTICIPACION_COBRO_HIJO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ_HIJO")]
        public string XX_COBRO_PARTICIPACION_NJ_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAGOS_HIJO")]
        public string XX_PAGOS_HIJO { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO_HIJO")]
        public string XX_INFORMATIVO_HIJO { get; set; }
        [XmlElement(ElementName = "CAT_NIVEL_2")]
        public List<CAT_NIVEL_2> CAT_NIVEL_2 { get; set; }
        [XmlElement(ElementName = "XX_ITEM_CLAVE_SAT_HIJO")]
        public string XX_ITEM_CLAVE_SAT_HIJO { get; set; }
        [XmlElement(ElementName = "PAQUETE_3")]
        public PAQUETE_3 PAQUETE_3 { get; set; }
        [XmlElement(ElementName = "XX_CLASIFICACION_PAGO_HIJO")]
        public string XX_CLASIFICACION_PAGO_HIJO { get; set; }
    }

    [XmlRoot(ElementName = "CAT_NIVEL_3")]
    public class CAT_NIVEL_3
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "NIVEL_3")]
    public class NIVEL_3
    {
        [XmlElement(ElementName = "ID_ITEM_HIJO")]
        public string ID_ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_HIJO")]
        public string ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_NUM")]
        public string ITEM_NUM { get; set; }
        [XmlElement(ElementName = "DESCRIPTION_HIJO")]
        public string DESCRIPTION_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV_HIJO")]
        public string XX_PAQUETE_INV_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO_HIJO")]
        public string XX_PARTICIPACION_COBRO_HIJO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ_HIJO")]
        public string XX_COBRO_PARTICIPACION_NJ_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAGOS_HIJO")]
        public string XX_PAGOS_HIJO { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO_HIJO")]
        public string XX_INFORMATIVO_HIJO { get; set; }
        [XmlElement(ElementName = "CAT_NIVEL_3")]
        public List<CAT_NIVEL_3> CAT_NIVEL_3 { get; set; }
        [XmlElement(ElementName = "XX_ITEM_CLAVE_SAT_HIJO")]
        public string XX_ITEM_CLAVE_SAT_HIJO { get; set; }
        [XmlElement(ElementName = "PAQUETE_4")]
        public PAQUETE_4 PAQUETE_4 { get; set; }
        [XmlElement(ElementName = "XX_CLASIFICACION_PAGO_HIJO")]
        public string XX_CLASIFICACION_PAGO_HIJO { get; set; }
    }

    [XmlRoot(ElementName = "CAT_PAQ_3")]
    public class CAT_PAQ_3
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "PAQUETE_3")]
    public class PAQUETE_3
    {
        [XmlElement(ElementName = "ORGANIZATION_ID")]
        public string ORGANIZATION_ID { get; set; }
        [XmlElement(ElementName = "NAME")]
        public string NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "XX_ITEM_CLAVE_SAT")]
        public string XX_ITEM_CLAVE_SAT { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV")]
        public string XX_PAQUETE_INV { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO")]
        public string XX_PARTICIPACION_COBRO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ")]
        public string XX_COBRO_PARTICIPACION_NJ { get; set; }
        [XmlElement(ElementName = "XX_PAGOS")]
        public string XX_PAGOS { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO")]
        public string XX_INFORMATIVO { get; set; }
        [XmlElement(ElementName = "NIVEL_3")]
        public List<NIVEL_3> NIVEL_3 { get; set; }
        [XmlElement(ElementName = "CAT_PAQ_3")]
        public List<CAT_PAQ_3> CAT_PAQ_3 { get; set; }
    }

    [XmlRoot(ElementName = "CAT_PAQ_4")]
    public class CAT_PAQ_4
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "PAQUETE_4")]
    public class PAQUETE_4
    {
        [XmlElement(ElementName = "ORGANIZATION_ID")]
        public string ORGANIZATION_ID { get; set; }
        [XmlElement(ElementName = "NAME")]
        public string NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV")]
        public string XX_PAQUETE_INV { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO")]
        public string XX_PARTICIPACION_COBRO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ")]
        public string XX_COBRO_PARTICIPACION_NJ { get; set; }
        [XmlElement(ElementName = "XX_PAGOS")]
        public string XX_PAGOS { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO")]
        public string XX_INFORMATIVO { get; set; }
        [XmlElement(ElementName = "NIVEL_4")]
        public List<NIVEL_4> NIVEL_4 { get; set; }
        [XmlElement(ElementName = "CAT_PAQ_4")]
        public CAT_PAQ_4 CAT_PAQ_4 { get; set; }
    }

    [XmlRoot(ElementName = "CAT_NIVEL_4")]
    public class CAT_NIVEL_4
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "NIVEL_4")]
    public class NIVEL_4
    {
        [XmlElement(ElementName = "ID_ITEM_HIJO")]
        public string ID_ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_HIJO")]
        public string ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_NUM")]
        public string ITEM_NUM { get; set; }
        [XmlElement(ElementName = "DESCRIPTION_HIJO")]
        public string DESCRIPTION_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV_HIJO")]
        public string XX_PAQUETE_INV_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO_HIJO")]
        public string XX_PARTICIPACION_COBRO_HIJO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ_HIJO")]
        public string XX_COBRO_PARTICIPACION_NJ_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAGOS_HIJO")]
        public string XX_PAGOS_HIJO { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO_HIJO")]
        public string XX_INFORMATIVO_HIJO { get; set; }
        [XmlElement(ElementName = "CAT_NIVEL_4")]
        public List<CAT_NIVEL_4> CAT_NIVEL_4 { get; set; }
        [XmlElement(ElementName = "XX_ITEM_CLAVE_SAT_HIJO")]
        public string XX_ITEM_CLAVE_SAT_HIJO { get; set; }
        [XmlElement(ElementName = "PAQUETE_5")]
        public PAQUETE_5 PAQUETE_5 { get; set; }
        [XmlElement(ElementName = "XX_CLASIFICACION_PAGO_HIJO")]
        public string XX_CLASIFICACION_PAGO_HIJO { get; set; }
    }


    [XmlRoot(ElementName = "CAT_PAQ_5")]
    public class CAT_PAQ_5
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "PAQUETE_5")]
    public class PAQUETE_5
    {
        [XmlElement(ElementName = "ORGANIZATION_ID")]
        public string ORGANIZATION_ID { get; set; }
        [XmlElement(ElementName = "NAME")]
        public string NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV")]
        public string XX_PAQUETE_INV { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO")]
        public string XX_PARTICIPACION_COBRO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ")]
        public string XX_COBRO_PARTICIPACION_NJ { get; set; }
        [XmlElement(ElementName = "XX_PAGOS")]
        public string XX_PAGOS { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO")]
        public string XX_INFORMATIVO { get; set; }
        [XmlElement(ElementName = "NIVEL_4")]
        public List<NIVEL_5> NIVEL_5 { get; set; }
        [XmlElement(ElementName = "CAT_PAQ_5")]
        public CAT_PAQ_5 CAT_PAQ_5 { get; set; }
    }

    [XmlRoot(ElementName = "CAT_NIVEL_5")]
    public class CAT_NIVEL_5
    {
        [XmlElement(ElementName = "INV")]
        public string INV { get; set; }
        [XmlElement(ElementName = "ORG")]
        public string ORG { get; set; }
        [XmlElement(ElementName = "CATEGORY_SET_ID")]
        public string CATEGORY_SET_ID { get; set; }
        [XmlElement(ElementName = "CATEGORY_ID")]
        public string CATEGORY_ID { get; set; }
        [XmlElement(ElementName = "CATALOG_CODE")]
        public string CATALOG_CODE { get; set; }
        [XmlElement(ElementName = "CATEGORY_CODE")]
        public string CATEGORY_CODE { get; set; }
    }

    [XmlRoot(ElementName = "NIVEL_5")]
    public class NIVEL_5
    {
        [XmlElement(ElementName = "ID_ITEM_HIJO")]
        public string ID_ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_HIJO")]
        public string ITEM_HIJO { get; set; }
        [XmlElement(ElementName = "ITEM_NUM")]
        public string ITEM_NUM { get; set; }
        [XmlElement(ElementName = "DESCRIPTION_HIJO")]
        public string DESCRIPTION_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAQUETE_INV_HIJO")]
        public string XX_PAQUETE_INV_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PARTICIPACION_COBRO_HIJO")]
        public string XX_PARTICIPACION_COBRO_HIJO { get; set; }
        [XmlElement(ElementName = "XX_COBRO_PARTICIPACION_NJ_HIJO")]
        public string XX_COBRO_PARTICIPACION_NJ_HIJO { get; set; }
        [XmlElement(ElementName = "XX_PAGOS_HIJO")]
        public string XX_PAGOS_HIJO { get; set; }
        [XmlElement(ElementName = "XX_INFORMATIVO_HIJO")]
        public string XX_INFORMATIVO_HIJO { get; set; }
        [XmlElement(ElementName = "CAT_NIVEL_5")]
        public List<CAT_NIVEL_5> CAT_NIVEL_5 { get; set; }
        [XmlElement(ElementName = "XX_ITEM_CLAVE_SAT_HIJO")]
        public string XX_ITEM_CLAVE_SAT_HIJO { get; set; }
        [XmlElement(ElementName = "XX_CLASIFICACION_PAGO_HIJO")]
        public string XX_CLASIFICACION_PAGO_HIJO { get; set; }
    }
}
