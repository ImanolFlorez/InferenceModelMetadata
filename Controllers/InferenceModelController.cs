namespace Adapting.Document.Controllers
{
    using Adapting.Core.Metadata;
    using Adapting.Core.Web.ActionFilters;
    using Adapting.Document.Services;
    using Microsoft.Practices.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using Adapting.Document.Domain;
    using Newtonsoft.Json;
    using Adapting.Core.Contents;
    using System.Net;
    using System.IO;
    using System.Xml;
    using InferenceModelMetadata.Json;
    using System.Configuration;
    using Adapting.Core.Membership;
    using System.Web.Script.Serialization;
    using WebserviceMetadata;
    using InferenceModelMetadata.Services;
    using InferenceModelMetadata.Domain;
    using System.Collections;

    [Area("Document")]
    public class InferenceModelController : Controller
    {
        /* private IAuditService auditService;
         private IAuditService AuditService { get { return this.auditService ?? (this.auditService = ServiceLocator.Current.GetInstance<IAuditService>()); } }
        */
        private IDocumentService documentService;
        private IDocumentService DocumentService { get { return this.documentService ?? (this.documentService = ServiceLocator.Current.GetInstance<IDocumentService>()); } }

        //----------------------------------------------------------------------------------------------------------------------------

        private IContentService contentService;
        private IContentService ContentService { get { return this.contentService ?? (this.contentService = ServiceLocator.Current.GetInstance<IContentService>()); } }

        //----------------------------------------------------------------------------------------------------------------------------

        private ICustomFieldService customFieldService;
        private ICustomFieldService CustomFieldService { get { return this.customFieldService ?? (this.customFieldService = ServiceLocator.Current.GetInstance<ICustomFieldService>()); } }

        //----------------------------------------------------------------------------------------------------------------------------

        private IUserService userService;
        private IUserService UserService { get { return this.userService ?? (this.userService = ServiceLocator.Current.GetInstance<IUserService>()); } }

        //----------------------------------------------------------------------------------------------------------------------------

        private IRepositoryService repositoryService;
        private IRepositoryService RepositoryService { get { return this.repositoryService ?? (this.repositoryService = ServiceLocator.Current.GetInstance<IRepositoryService>()); } }

        //----------------------------------------------------------------------------------------------------------------------------


        //----------------------------------------------------------------------------------------------------------------------------

        public string ConnectWebService(string JSON, string url, string authorization)
        {

            //Creamos un Objeto tipo Application que tienes las clases a desearilizar del JSON
            //Crea la Conexion con el webservice
            var request = (HttpWebRequest)WebRequest.Create(url + "API/Iconos/");
            request.Method = "POST";
            request.Timeout = 1000000;
            //Agrega el token de autorizacion
            if (authorization != null)
            {
                request.Headers.Add("Authorization", authorization);
            }
            
            //Escribe el JSON en el cuerpo de la peticion
            using (var writter = new StreamWriter(request.GetRequestStream()))
            {
                writter.Write(JSON);
                writter.Close();
            }
            //Realiza la Peticion
            var httpResponse = (HttpWebResponse)request.GetResponse();
            //Retornamos el status de la peticion
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            //Leemos el resultado arrojado de la peticion
            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = reader.ReadToEnd();//Capturamos el JSON devuelto por el webservice
                DateTime now = DateTime.Now;//Capturamos la fecha actual
                //Validamos que el resultado arrojado por el webservice no sea nulo
                if (result != null)
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------

        public string LoadXmlRouteInferenceModel(string Key)
        //------------------------------------Retorna la ruta del fichero de configuracion de la llave anteriormente ingresa------------------------------------------------------------------------------------------

        {
            var appSettings = ConfigurationManager.AppSettings;
            if (!appSettings.HasKeys())
            {
                throw new ConfigurationErrorsException("No existen entradas de configuración");
            }
            var keyvalues = appSettings.GetValues(Key);
            if (keyvalues == null)
            {
                throw new ConfigurationErrorsException(string.Format("No existe una entrada de configuración en appSettings.config para la clave {0}", Key));
            }
            var customSetting = keyvalues[0];
            if (customSetting == null)
            {
                throw new ConfigurationErrorsException(string.Format("No existe una entrada de configuración en appSettings.config para la clave {0}", Key));
            }
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var fileRoute = Path.Combine(baseDirectory, customSetting);
            if (!System.IO.File.Exists(fileRoute))
            {
                throw new FileNotFoundException("El fichero de configuración no existe en Config", fileRoute);
            }
            return fileRoute;
        }

        //----------------------------------------------------------------------------------------------------------------------------


        public XmlDocument ReaderXml(string file)
        //---------------------------------Funcion que lee el archivo de configuracion y lo devuelve cargado en un XmlDocument--------------------------------  
        {
            var xmlDocument = new XmlDocument();
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.IgnoreComments = true;
            var xmlReader = XmlReader.Create(file, xmlReaderSettings);
            xmlDocument.Load(xmlReader);
            return xmlDocument;
        }

        //----------------------------------------------------------------------------------------------------------------------------

        //Funcion que recibe un Objeto tipo XmlDocument y el Nombre del modelo de Inferencia                                          
        //Lee el XML de configuracion y Retorna los campos en un Array de tipo XmlNodeList                                                            
        public XmlNodeList GetElementXml(XmlDocument xmlDocument, string model, string Hashtag)
        {
            XmlNodeList Models = xmlDocument.GetElementsByTagName("Models");
            XmlNodeList Model = ((XmlElement)Models[0]).GetElementsByTagName("Model");
            foreach (XmlElement f in Model)
            {
                if (f.GetAttribute("name").ToLower().Equals(model.ToLower()))
                {
                    return f.GetElementsByTagName(Hashtag);
                }
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public string GetElementXmlDecision(XmlDocument xmlDocument, string model, string Code, string holdercode)
        //--------------------------Funcion que lee el XML de confiabilidad y retorna una cadena con los valores a filtrar------------------------------- 
        {
            XmlNodeList Models = xmlDocument.GetElementsByTagName("Models");
            XmlNodeList Model = ((XmlElement)Models[0]).GetElementsByTagName("Model");
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document doc = new Document();
            if (caseFolder != null)
            {
                var holders = caseFolder.CaseFolderHolders;
                if (holders != null && holders.Any())
                {
                    holders = holders.Where(x => x.CaseFolderElement.Code.Equals(holdercode)).OrderByDescending(x => x.Document.DateModified).ToList();
                    if (holders.Any())
                    {
                        foreach (var holder in holders)
                        {
                            if (holder.Document.IsActive)
                            {
                                doc = holder.Document;
                                break;
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                doc = DocumentService.GetDocument(Code);
            }
            foreach (XmlElement f in Model)
            {
                if (f.GetAttribute("Name").ToLower().Equals(model.ToLower()))
                {
                    
                    foreach (XmlElement i in f.GetElementsByTagName("Metadata"))
                    {
                        var fv = doc.GetFieldValue(i.GetAttribute("Code"));
                        if (fv != null)
                        {
                            foreach (XmlElement j in i.GetElementsByTagName("Condicion"))
                            {
                                var value = (fv.Value != null) ? fv.Value.ToLower() : null;
                                if (value.Contains(j.InnerText.ToLower()))
                                {
                                    var LimAuxC = j.GetAttribute("Confiable").Split(',');
                                    var LimAuxN = j.GetAttribute("NoConfiable").Split(',');
                                    LimAuxC = LimAuxC.Distinct().ToArray();
                                    LimAuxN = LimAuxN.Distinct().ToArray();
                                    if (LimAuxC.Any(x => x == "*"))
                                    {
                                        if (LimAuxN.Any(x => x == "*"))
                                        {
                                            return null;
                                        }
                                        else
                                        {
                                            return "*~" + string.Join(",", LimAuxN);
                                        }
                                    }
                                    else if (LimAuxN.Any(x => x == "*"))
                                    {
                                        if (LimAuxC.Any(x => x == "*"))
                                        {
                                            return null;
                                        }
                                        else
                                        {
                                            return string.Join(",", LimAuxC) + "~*";
                                        }
                                    }
                                    else
                                    {
                                        return string.Join(",", LimAuxC) + "~" + string.Join(",", LimAuxN);
                                    }
                                }
                            }
                        }
                        
                    }
                }
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public string Confiable(string cadena, string valor)
        //--------------------------------Funcion que calcula si una prediccion es confiable o no---------------------------------------------------------
        {
            if (cadena != null)
            {
                var Variable1 = cadena.Split('~');
                var Var1 = Variable1[0];
                var Var2 = Variable1[1];

                if (Var1 == "")
                {
                    if (Var2 != "")
                    {
                        var noconfiable = Var2.Split(',');
                        if (noconfiable.Any(x => x == "*") || noconfiable.Any(x => x == valor))
                        {
                            //return "<img src='/Content/Images/error.png'>";
                            return "false";
                        }
                        else
                        {
                            //return "<img src='/Content/Images/help.png'>";
                            return null;
                        }
                    }
                    else
                    {
                        //return "<img src='/Content/Images/help.png'>";
                        return null;
                    }
                }
                else if (Var2 == "")
                {
                    if (Var1 != "")
                    {
                        var confiable = Var1.Split(',');
                        if (confiable.Any(x => x == "*") || confiable.Any(x => x == valor))
                        {
                            //return "<img src='/Content/Images/approve.png'>";
                            return "true";
                        }
                        else
                        {
                            //return "<img src='/Content/Images/help.png'>";
                            return null;
                        }
                    }
                    else
                    {
                        //return "<img src='/Content/Images/help.png'>";
                        return null;
                    }
                }
                else
                {
                    var confiable = Var1.Split(',');
                    var noconfiable = Var2.Split(',');
                    if (confiable.Any(x => x == "*"))
                    {
                        if (noconfiable.Any(x => x == valor))
                        {
                            //return "<img src='/Content/Images/error.png'>";
                            return "false";
                        }
                        else
                        {
                            //return "<img src='/Content/Images/approve.png'>";
                            return "true";
                        }
                    }
                    else
                    {
                        if (noconfiable.Any(x => x == "*"))
                        {
                            if (confiable.Any(x => x == valor))
                            {
                                //return "<img src='/Content/Images/approve.png'>";
                                return "true";
                            }
                            else
                            {
                                return "false";
                                //return "<img src='/Content/Images/error.png'>";
                            }
                        }
                        else
                        {
                            if (confiable.Any(x => x == valor))
                            {
                                //return "<img src='/Content/Images/approve.png'>";
                                return "true";
                            }
                            else if (noconfiable.Any(x => x == valor))
                            {
                                return "false";
                                //return "<img src='/Content/Images/error.png'>";
                            }
                            else
                            {
                                //return "<img src='/Content/Images/help.png'>";
                                return null;
                            }
                        }
                    }
                }
            }
            else
            {
                //return "<img src='/Content/Images/help.png'>";
                return null;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public string Fiable(bool fiable)
        {
            if (fiable) {
                return "<img src='/Content/Images/approve.png'>";
            }
            else
            {
                return "<img src='/Content/Images/error.png'>";
            }
        }
         public string GetTypeSupervision(XmlDocument xmlDocument, string model)
        {
            XmlNodeList Models = xmlDocument.GetElementsByTagName("Models");
            XmlNodeList Model = ((XmlElement)Models[0]).GetElementsByTagName("Model");
            foreach (XmlElement f in Model)
            {
                if (f.GetAttribute("name").ToLower().Equals(model.ToLower()))
                {
                    return f.GetAttribute("Supervision") + ";" + f.GetAttribute("MaxNeg") + ";" + f.GetAttribute("MinPos"); 
                }
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------
        [RequiresPermission("")]
        public ActionResult SelectModel(string Cadena, string FieldId, string Code, string Fieldname, string Minvalue)
        {
            try
            {
                int.TryParse(FieldId, out int id);
                var Field = CustomFieldService.GetField(id);
                var caseFolder = DocumentService.GetCaseFolder(Code);
                Document document = new Document();
                StaticContent staticContent = null;
                staticContent = ContentService.GetContentByCode(Code);
                var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
                var user = HttpContext.User.Identity.Name;
                var Database = "";
                var ServiceAudit = new AuditService();
                DateTime now = DateTime.Now;
                var uricontroller = InferenceModelField.GetPath("Confirmation");//Cargamos el Controlador para ejecutar la siguiente funcion
                Cadena += now + ";" + user;
                var Select = Cadena.Split(';');
                var values = fieldvalue.Value.Split('ß');
                if (values.Length > 2 && values.Length < 4)
                {
                    for(var j = 0;j < values.Length;j++)
                    {
                        Database += values[j] + "ß";
                    }
                    Database += Cadena;
                }
                Guid.TryParse(values[0], out Guid Id);
                Audit audit = ServiceAudit.GetAudit(Id);
                audit.AreaSelect = Select[0];
                audit.PorConfSelect = Select[2];
                audit.FiableSelect = Select[3];
                audit.ConfiableSelect = Select[4];
                audit.PositionSelect = Select[5];
                audit.FechaSelect = Select[6];
                audit.AuthorSelect = Select[7];
                audit.TypeAction = "Manual";
                var a = ServiceAudit.InsertOrUpdateAudit(audit);
                if (caseFolder != null)
                {
                    caseFolder.SetFieldValue(Field, Database);
                }
                else
                {
                    document = DocumentService.GetDocument(Code);
                    document.SetFieldValue(Field, Database);
                }
                var script = "<script type=\"text/javascript\"> " +
                      "$(\".InferenceModelButton_" + Fieldname + "\").hide(); " +
                      "</script>";
                var cadena = Cadena.Split(';')[0];
                cadena += "<p>Confirmar?</p><input type=\"button\" id=\"approve\" value=\"Si\" onclick=\"javascript:InferenceModel.Confirmation('" + Cadena + "','" + uricontroller + "','" + Fieldname + "','" + Code + "','" + FieldId + "','True','" + Minvalue + "')\"><input type=\"button\" id=\"inapprove\" value=\"No\" onclick=\"javascript:InferenceModel.Confirmation('" + Cadena + "','" + uricontroller + "','" + Fieldname + "','" + Code + "','" + FieldId + "','False','" + Minvalue + "')\">";
                return Json(script+cadena);
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }
        [RequiresPermission("")]
        public ActionResult Confirmation(string Cadena, string Selection, string Code, string FielId, string Fieldname, string Minvalue, string Holdercode)
        {
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document document = new Document();
            int.TryParse(FielId, out int id);
            var Field = CustomFieldService.GetField(id);
            StaticContent staticContent = null;
            staticContent = ContentService.GetContentByCode(Code);
            var ServiceAudit = new AuditService();
            var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
            DateTime now = DateTime.Now;
            var Database = "";
            var user = HttpContext.User.Identity.Name;
            var values = fieldvalue.Value.Split('ß');
            if (values.Length >= 4)
            {
                if (Selection.ToLower().Equals("true"))
                {
                    Cadena = "true;;;;;;" + now + ";" + user;
                    Database=values[3];
                    var Supervisor = Cadena.Split(';');
                    Guid.TryParse(values[0], out Guid Id);
                    Audit audit = ServiceAudit.GetAudit(Id);
                    audit.ApproveSuper = Supervisor[0];
                    audit.AreaSuper = Supervisor[1];
                    audit.PorConfSuper = Supervisor[2];
                    audit.FiableSuper = Supervisor[3];
                    audit.ConfiableSuper = Supervisor[4];
                    audit.PositionSuper = Supervisor[5];
                    audit.FechaSuper = Supervisor[6];
                    audit.AuthorSuper = Supervisor[7];
                    var a = ServiceAudit.InsertOrUpdateAudit(audit);
                    if (caseFolder != null)
                    {
                        caseFolder.SetFieldValue(Field, Database + ";" + a.LogId);   
                    }
                    else
                    {
                        document = DocumentService.GetDocument(Code);
                        document.SetFieldValue(Field, Database + ";" + a.LogId); 
                    }
                }
                else
                {
                    return Json(CreatedTable(values[1], "Yes", Fieldname, Code, FielId, 15, Minvalue, Holdercode));
                }
            }
            return Json(Database.Split(';')[0]);
        }

        //----------------------------------------------------------------------------------------------------------------------------
        [RequiresPermission("")]
        public ActionResult FirstCall(string Code, string MinValue, string DefaultValue, string MaxValue, string Fieldname, string FieldId, string Holdercode)
        {
            var pathxmlInferenceModelEquivalence = LoadXmlRouteInferenceModel("EquivalenceInferenceModelXml");
            XmlDocument xmlInferenceModelEquivalence = ReaderXml(pathxmlInferenceModelEquivalence);
            var Supervision = GetTypeSupervision(xmlInferenceModelEquivalence, MinValue).Split(';');
            StaticContent staticContent = null;
            staticContent = ContentService.GetContentByCode(Code);
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);


            if (!string.IsNullOrEmpty(fieldvalue.Value))
            {
                var script = "<script type=\"text/javascript\"> " +
                     "$(\".InferenceModelButton_" + Fieldname + "\").hide(); " +
                     "</script>";
                var cadena = fieldvalue.Value.Split('ß');
                if (Supervision[0] == "Selection")
                {
                    if (cadena.Length > 1 && cadena.Length < 4)
                    {
                        return Json(CreatedTable(cadena[1], null, Fieldname, Code, FieldId, int.Parse(MaxValue), MinValue, Holdercode));
                    }
                    else if (cadena.Length > 3 && cadena.Length < 5)
                    {
                        var uricontroller = InferenceModelField.GetPath("Confirmation");//Cargamos el Controlador para ejecutar la siguiente funcion
                        var d = "<p>Confirmar?</p><input type=\"button\" class=\"approve\" value=\"Si\" onclick=\"javascript:InferenceModel.Confirmation('" + cadena[2] + "','" + uricontroller + "','" + Fieldname + "','" + Code + "','" + FieldId + "','True','" + MinValue + "','" + Holdercode + "')\"><input type=\"button\" class=\"inapprove\" value=\"No\" onclick=\"javascript:InferenceModel.Confirmation('" + cadena[2] + "','" + uricontroller + "','" + Fieldname + "','" + Code + "','" + FieldId + "','False','" + MinValue + "','" + Holdercode + "')\">";
                        return Json(script + cadena[3].Split(';')[0] + d);
                    }
                    else if (cadena.Length >= 4)
                    {
                        if (cadena[4].Contains("true"))
                        {
                            return Json(script + cadena[3]);
                        }
                        else
                        {
                            return Json(script + cadena[4]);
                        }
                    }
                    if (fieldvalue.Value.Split(';').Length > 1)
                    {
                        return Json(script + fieldvalue.Value.Split(';')[0]);
                    }
                }
                else if(Supervision[0] == "Qualification")
                {
                    if (cadena.Length > 3)
                    {
                        return Json(script + CreatedTable(cadena[1], null, Fieldname, Code, FieldId, int.Parse(MaxValue), MinValue, Holdercode,qualification:cadena[3]));
                    }
                    else if(cadena.Length>1)
                    {
                        return Json(CreatedTable(cadena[1], null, Fieldname, Code, FieldId, int.Parse(MaxValue), MinValue, Holdercode));
                    }
                }
                
            }
            return Json("");
        }

        //----------------------------------------------------------------------------------------------------------------------------

        private byte[] GetBytes(Stream file)
        {
            byte[] myBinary = null;
            if (file != null)
            {
                myBinary = new byte[file.Length];
                file.Read(myBinary, 0, (int)file.Length);
                return myBinary;
            }
            return myBinary;
        }

        //----------------------------------------------------------------------------------------------------------------------------


        public string GetMetadatos(string Code, string MinValue, string Holdercode)
        {
            var bandera = false;
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document doc = new Document();
            if (caseFolder != null)
            {
                var holders = caseFolder.CaseFolderHolders;
                if (holders != null && holders.Any())
                {
                    holders = holders.Where(x => x.CaseFolderElement.Code.Equals(Holdercode)).OrderByDescending(x => x.Document.DateModified).ToList();
                    if (holders.Any())
                    {
                        foreach (var holder in holders)
                        {
                            if (holder.Document.IsActive)
                            {
                                bandera = true;
                                doc = holder.Document;
                                break;
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                doc = DocumentService.GetDocument(Code);
            }

            var pathInferenceModelConfig = LoadXmlRouteInferenceModel("InferenceModelXml");//Obtiene el fichero de configuracion
            XmlDocument xmlInferenceModelConfig = ReaderXml(pathInferenceModelConfig);//Obtiene el XML parseable
            string[] Array = GetModels(xmlInferenceModelConfig);
            var a = Array.Any(x => x == MinValue);
            var f = (doc.MainFile != null) ? doc.MainFile : null;
            if (!a)
            {
                return null;
            }
            var FieldArray = GetElementXml(xmlInferenceModelConfig, MinValue, "Field");  
            var Metadata = new Dictionary<string, Object>();
            var File = new Dictionary<string, Object>();
            var Model = new Dictionary<string, Object>();
            var JSON = new Dictionary<string, Object>();
            /*
            Metadata.Add("Modo", (doc.Mode == DocumentRegistryMode.In) ? "E" : (doc.Mode == DocumentRegistryMode.Out) ? "S" : "I");
            Metadata.Add("Entrada", (doc.Mode == DocumentRegistryMode.In) ? "TRUE" : "FALSE");
            Metadata.Add("Salida", (doc.Mode == DocumentRegistryMode.Out) ? "TRUE" : "FALSE");
            Metadata.Add("Code", (doc.EntityType.Code != null) ? doc.EntityType.Code : "NA");
            Metadata.Add("name", (doc.Title.SingleString != null) ? doc.Title.SingleString : "NA");
            Metadata.Add("datecreated", (doc.DateCreated.ToString("s") != null) ? doc.DateCreated.ToString("s") : "NA");
            Metadata.Add("datemodified", (doc.DateModified.ToString("s") != null) ? doc.DateModified.ToString("s") : "NA");
            Metadata.Add("author", (doc.Author != null) ? doc.Author : "NA");
            Metadata.Add("NombreFichero", (doc.Name != null) ? doc.Name : "NA");
            Metadata.Add("contentid", (doc.Id.ToString() != null) ? doc.Id.ToString() : "NA");
            Metadata.Add("mimetype", (f != null) ? f.MimeType !=null ? f.MimeType :"NA" : "NA");
            Metadata.Add("size", (f != null) ? Convert.ToInt32(f.Size):1);
            Metadata.Add("path", (f != null) ? (f.Path.ToString() != null) ? f.Path.ToString() : "NA" : "NA");
            Metadata.Add("area", "NA");
            Metadata.Add("CodigoTipoDocumental", (doc.EntityType.Code != null) ? doc.EntityType.Code : "NA");
            Metadata.Add("NombreTipoDocumental", (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Name.SingleString : "NA");
            Metadata.Add("CodigoPadre", (((Folder)doc.Parent).Code != null) ? ((Folder)doc.Parent).Code : "NA");
            Metadata.Add("NombrePadre", (((Folder)doc.Parent).Title.SingleString != null) ? ((Folder)doc.Parent).Title.SingleString : "NA");
            Metadata.Add("CodigoAbuelo", (((Folder)doc.Parent.Parent)?.Code != null) ? ((Folder)doc.Parent.Parent)?.Code : "NA");
            Metadata.Add("NombreAbuelo", (((Folder)doc.Parent.Parent)?.Title.SingleString != null) ? ((Folder)doc.Parent.Parent)?.Title.SingleString : "NA");
            Metadata.Add("en_expte", (doc.Parent is CaseFolder) ? "TRUE" : "FALSE");
            Metadata.Add("CodigoSerieDocumental", (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Code : "NA");
            Metadata.Add("NombreSerieDocumental", (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Name.SingleString : "NA");*/
            if (FieldArray != null)
            {
                foreach (XmlElement field in FieldArray)
                {
                    switch (field.GetAttribute("type").ToLower())
                    {
                    case "organization":
                        Guid orgGuid;
                        var orgName = "NA";
                        if (field.InnerText != "")
                        {
                            var fo = doc.GetFieldValue(field.InnerText);
                            if (fo != null)
                            {
                                if ((Guid.TryParse(fo.Value, out orgGuid)))
                                {
                                    var org = UserService.GetOrganization(orgGuid);
                                    if (org != null)
                                    {
                                        orgName = org.Code;
                                    }
                                }
                            }
                        }
                        Metadata.Add(field.GetAttribute("name"), orgName);
                        break;
                    case "text":
                        var text = "NA";
                        if (field.InnerText != "")
                        {
                            var ft = doc.GetFieldValue(field.InnerText);
                            if (ft != null)
                            {
                                if (bandera)
                                {
                                    text = ft.Field.GetFieldRawValue(ft.Value, caseFolder);
                                }
                                else
                                {
                                    text = ft.Field.GetFieldRawValue(ft.Value, doc);
                                }
                            }
                        }
                        Metadata.Add(field.GetAttribute("name"), text);
                        break;
                    case "number":
                        var number = "NA";
                        if (field.InnerText != "")
                        {
                            var fn = doc.GetFieldValue(field.InnerText);
                            if (fn != null)
                            {
                                if (bandera)
                                {
                                    number = fn.Field.GetFieldRawValue(fn.Value, caseFolder);
                                }
                                else
                                {
                                    number = fn.Field.GetFieldRawValue(fn.Value, doc);
                                }
                            }
                        }
                        Metadata.Add(field.GetAttribute("name"), number);
                        break;
                    case "date":
                        var date = "NA";
                        if (field.InnerText != "")
                        {
                            var fd = doc.GetFieldValue(field.InnerText);
                            if (fd != null)
                            {
                                if (bandera)
                                {
                                    number = fd.Field.GetFieldRawValue(fd.Value, caseFolder);
                                }
                                else
                                {
                                    number = fd.Field.GetFieldRawValue(fd.Value, doc);
                                }
                            }
                        }
                        Metadata.Add(field.GetAttribute("name"), date);
                        break;
                    case "boolean":
                        var boolean = "NA";
                        if (field.InnerText != "")
                        {
                            var fb = doc.GetFieldValue(field.InnerText);
                            if (fb != null)
                            {
                                if (bandera)
                                {
                                    number = fb.Field.GetFieldRawValue(fb.Value, caseFolder);
                                }
                                else
                                {
                                    number = fb.Field.GetFieldRawValue(fb.Value, doc);
                                }
                            }
                        }
                        Metadata.Add(field.GetAttribute("name"), boolean);
                        break;
                    case "document":
                        switch (field.InnerText)
                        {
                        case "1":
                            Metadata.Add(field.GetAttribute("name"), (doc.Mode == DocumentRegistryMode.In) ? "E" : (doc.Mode == DocumentRegistryMode.Out) ? "S" : "I");
                            break;
                        case "2":
                            Metadata.Add(field.GetAttribute("name"), (doc.Mode == DocumentRegistryMode.In) ? "TRUE" : "FALSE");
                            break;
                        case "3":
                            Metadata.Add(field.GetAttribute("name"), (doc.Mode == DocumentRegistryMode.Out) ? "TRUE" : "FALSE");
                            break;
                        case "4":
                            Metadata.Add(field.GetAttribute("name"), doc.EntityType.Code ?? "NA");
                            break;
                        case "5":
                            Metadata.Add(field.GetAttribute("name"), doc.Title.SingleString ?? "NA");
                            break;
                        case "6":
                            Metadata.Add(field.GetAttribute("name"), doc.DateCreated.ToString("s") ?? "NA");
                            break;
                        case "7":
                            Metadata.Add(field.GetAttribute("name"), doc.DateModified.ToString("s") ?? "NA");
                            break;
                        case "8":
                            Metadata.Add(field.GetAttribute("name"), doc.Author ?? "NA");
                            break;
                        case "9":
                            Metadata.Add(field.GetAttribute("name"), doc.Name ?? "NA");
                            break;
                        case "10":
                            Metadata.Add(field.GetAttribute("name"), doc.Id.ToString() ?? "NA");
                            break;
                        case "11":
                            Metadata.Add(field.GetAttribute("name"), (f != null) ? f.MimeType ?? "NA" : "NA");
                            break;
                        case "12":
                            Metadata.Add(field.GetAttribute("name"), (f != null) ? Convert.ToInt32(f.Size) : 1);
                            break;
                        case "13":
                            Metadata.Add(field.GetAttribute("name"), (f != null) ? f.Path.ToString() ?? "NA" : "NA");
                            break;
                        case "14":
                            Metadata.Add(field.GetAttribute("name"), "NA");
                            break;
                        case "15":
                            Metadata.Add(field.GetAttribute("name"), doc.EntityType.Code ?? "NA");
                            break;
                        case "16":
                            Metadata.Add(field.GetAttribute("name"), (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Name.SingleString : "NA");
                            break;
                        case "17":
                            Metadata.Add(field.GetAttribute("name"), ((Folder)doc.Parent).Code ?? "NA");
                            break;
                        case "18":
                            Metadata.Add(field.GetAttribute("name"), ((Folder)doc.Parent).Title.SingleString ?? "NA");
                            break;
                        case "19":
                            Metadata.Add(field.GetAttribute("name"), (((Folder)doc.Parent.Parent)?.Code) ?? "NA");
                            break;
                        case "20":
                            Metadata.Add(field.GetAttribute("name"), (((Folder)doc.Parent.Parent)?.Title.SingleString) ?? "NA");
                            break;
                        case "21":
                            Metadata.Add(field.GetAttribute("name"), (doc.Parent is CaseFolder) ? "TRUE" : "FALSE");
                            break;
                        case "22":
                            Metadata.Add(field.GetAttribute("name"), (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Code : "NA");
                            break;
                        case "23":
                            Metadata.Add(field.GetAttribute("name"), (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).EntityType.Name.SingleString : "NA");
                            break;
                        case "24":
                            Metadata.Add(field.GetAttribute("name"), (doc.Parent is CaseFolder) ? ((Folder)doc.Parent).Code : "NA");
                            break;
                        default:
                            Metadata.Add(field.GetAttribute("name"), "NA");
                            break;
                        }    
                        break;
                    default:
                        Metadata.Add(field.GetAttribute("name"), "NA");
                        break;
                    } 
                }
            }
           
            string base64 = null;
            
            if (f!=null)
            {
                var repName = f.RepositoryName;
                var rep = RepositoryService.GetRepository(repName);
                var file = rep.GetContent(doc.MainFile.Path);
                base64 = (file != null) ? Convert.ToBase64String(GetBytes(file)) : null;
                var extension = Path.GetExtension(doc.MainFile.Name);
                
                if (base64 != null)
                {
                    File.Add("Name", doc.MainFile.Name);
                    File.Add("Extension", extension.ToLower());
                    File.Add("name", (doc.Title.SingleString != null) ? doc.Title.SingleString : "NA");
                    File.Add("path",(f != null) ? (f.Path.ToString() != null) ? f.Path.ToString() : "NA" : "NA");
                    File.Add("Code", (doc.EntityType.Code != null) ? doc.EntityType.Code : "NA");
                    File.Add("Byte", base64);
                    JSON.Add("File", File);
                }

            }
            Model.Add("Name", MinValue);
            JSON.Add("Metadata", Metadata);
            JSON.Add("Model", Model);
            var jsonSerializer = new JavaScriptSerializer();
            jsonSerializer.MaxJsonLength = 500000000;
            return jsonSerializer.Serialize(JSON);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        [RequiresPermission("")]
        public ActionResult Consultbutton(string Code, string MinValue, string DefaultValue, string MaxValue, string Fieldname, string FieldId, string Holdercode)
        {
            var Id = string.Empty;
            var DocumentCode = string.Empty;
            var DocumentId = string.Empty;
            var CaseFolderId = string.Empty;
            var CaseFolderCode = string.Empty;
            var Holder = string.Empty;
            var fieldId = FieldId;
            var fieldCode = string.Empty;

            //var audit = InsertAudit("Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na", "Na");
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document doc = new Document();
            if (caseFolder != null)
            {
                CaseFolderCode = caseFolder.Code;
                CaseFolderId = caseFolder.Id.ToString();
                var holders = caseFolder.CaseFolderHolders;
                if (holders != null && holders.Any())
                {
                    holders = holders.Where(x => x.CaseFolderElement.Code.Equals(Holdercode)).OrderByDescending(x => x.Document.DateModified).ToList();
                    if (holders.Any())
                    {
                        Holder = Holdercode;
                        foreach (var holder in holders)
                        {
                            if (holder.Document.IsActive)
                            {
                                doc = holder.Document;
                                break;
                            }
                        }
                    }
                    else
                    {
                        return Json("<p> No se encontro el Holder especificado </p>");
                    }
                }
            }
            else
            {
                doc = DocumentService.GetDocument(Code);
            }
            Application decision;
            var message ="";
            var ServiceAudit = new AuditService();
            DocumentCode = doc.Code;
            DocumentId = doc.Id.ToString();
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            fieldCode = Field.Code;
            fieldId = Field.Id.ToString();
            Audit Register;
            string json = GetMetadatos(Code, MinValue, Holdercode);
            if (string.IsNullOrEmpty(json))
            {
                return Json("<p> Modelo Ingresado No Existe </p>");
            }
            string result;
            try
            {
                var Login = LoginSession(DefaultValue);
                if (!string.IsNullOrEmpty(Login))
                {
                    result = ConnectWebService(json, DefaultValue, Login);
                }
                else
                {
                    result = null;
                }
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
                result = null;
            }
             
            if (result != null)
            {
                decision = JsonConvert.DeserializeObject<Application>(result);//Deserializa el JSON y lo mapea
                if (bool.Parse(decision.message))
                {
                    if (!string.IsNullOrEmpty(Holder))
                    {
                        Register = ServiceAudit.SelectRegisterIsCaseFolder(CaseFolderId, fieldId, Holder);
                        if (Register != null)
                        {
                            ServiceAudit.Remove(Register);
                        }
                        Audit audit = new Audit
                        {
                            DocumentCode = DocumentCode,
                            DocumentId = DocumentId,
                            FieldCode = fieldCode,
                            FieldId = fieldId,
                            CaseFolderCode = CaseFolderCode,
                            CaseFolderId = CaseFolderId,
                            HolderCode = Holder,
                            InferenceModel = MinValue,
                            LogId= decision.logid
                        };
                        Register = ServiceAudit.InsertOrUpdateAudit(audit);
                        Id = Register.Id.ToString();
                        caseFolder.SetFieldValue(Field, Id + "ß" + result);
                    }
                    else
                    {
                        Register = ServiceAudit.SelectRegisterIsDocument(DocumentId, fieldId);
                        if (Register != null)
                        {
                            ServiceAudit.Remove(Register);
                        }
                        Audit audit = new Audit
                        {
                            DocumentCode = DocumentCode,
                            DocumentId = DocumentId,
                            FieldCode = fieldCode,
                            FieldId = fieldId,
                            CaseFolderCode = CaseFolderCode,
                            CaseFolderId = CaseFolderId,
                            HolderCode = Holder,
                            InferenceModel = MinValue,
                            LogId = decision.logid
                        };
                        Register = ServiceAudit.InsertOrUpdateAudit(audit);
                        Id = Register.Id.ToString();
                        doc.SetFieldValue(Field, Id + "ß" + result);
                    }

                    var table = CreatedTable(result, null, Fieldname, Code, FieldId, int.Parse(MaxValue), MinValue, Holdercode);
                    return Json(table != null ? table : "<p> Sin Resultados1 </p>");
                }
                else
                {
                    return Json("<p> Sin Resultados2 </p>");
                }
            } 
            else
            {
                return Json("<p> Sin Resultados3 "+ message + " </p>");
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public string ConsultAction(string Code, string MinValue, string DefaultValue, string MaxValue, string Fieldname, string FieldId, string CaseFolderCode)
        {
            var doc = DocumentService.GetDocument(Code);
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            string json = GetMetadatos(Code, MinValue, CaseFolderCode);
            if (json == null)
            {
                return null;
            }
            var Login = LoginSession(DefaultValue);
            try
            {
                if (Login != null)
                {
                    var result = ConnectWebService(json, DefaultValue, Login);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------
        public ActionResult ConsultSuper(string Cadena, string FieldId, string Code, string Fieldname, string Minvalue)
        {
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            var user = HttpContext.User.Identity.Name;
            DateTime now = DateTime.Now;
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document document = new Document();
            StaticContent staticContent = null;
            staticContent = ContentService.GetContentByCode(Code);
            var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
            var ServiceAudit = new AuditService();
            Cadena = Cadena+"false;" + now + ";" + user;
            var Database = "";
            var values = fieldvalue.Value.Split('ß');
            if (values.Length >= 4)
            {
                    Database = Cadena;
                    var Supervisor = Cadena.Split(';');
                    Guid.TryParse(values[0], out Guid Id);
                    Audit audit = ServiceAudit.GetAudit(Id);
                    audit.ApproveSuper = Supervisor[6];
                    audit.AreaSuper = Supervisor[0];
                    audit.PorConfSuper = Supervisor[2];
                    audit.FiableSuper = Supervisor[3];
                    audit.ConfiableSuper = Supervisor[4];
                    audit.PositionSuper = Supervisor[5];
                    audit.FechaSuper = Supervisor[7];
                    audit.AuthorSuper = Supervisor[8];
                if (caseFolder != null)
                {
                    var a = ServiceAudit.InsertOrUpdateAudit(audit);
                    caseFolder.SetFieldValue(Field, Database + ";" + a.LogId);
                    
                }
                else
                {
                    var a = ServiceAudit.InsertOrUpdateAudit(audit);
                    document = DocumentService.GetDocument(Code);
                    document.SetFieldValue(Field, Database + ";" + a.LogId);
                    
                }
            }
            return Json(Cadena.Split(';')[0]);
        }

        //Funcion que lee el archivo de configuracion y lo devuelve cargado en un XmlDocument  
        //Funcion que recibe un Objeto de tipo XmlDocument
        //Lee el XML configuracion y retorna los modelos existentes en un Array
        public static string[] GetModels(XmlDocument xmlDocument)
        {
            var NameModel = "";
            XmlNodeList Models = xmlDocument.GetElementsByTagName("Models");
            XmlNodeList Model = ((XmlElement)Models[0]).GetElementsByTagName("Model");
            foreach (XmlElement f in Model)
            {
                NameModel += (f.GetAttribute("name") + ";");
            }
            return NameModel.Split(';');
        }

        //----------------------------------------------------------------------------------------------------------------------------
        public string CreatedTable(string result, string confirmation, string fieldname, string codedoc, string fieldId, int maxvalue, string MinValue, string holdercode,string qualification=null)
        {
            //Cargamos la configuracion para los valores de equivalencias
            var pathxmlInferenceModelEquivalence = LoadXmlRouteInferenceModel("EquivalenceInferenceModelXml");
            XmlDocument xmlInferenceModelEquivalence = ReaderXml(pathxmlInferenceModelEquivalence);
            var Supervision = GetTypeSupervision(xmlInferenceModelEquivalence, MinValue).Split(';');
            var FieldArray = GetElementXml(xmlInferenceModelEquivalence, MinValue, "Category");
            //Cargamos la Configuracion para calcular la confiabilidad de las predicciones
            var pathxmlValidateCondicionInferenceModel = LoadXmlRouteInferenceModel("ValidateResultsInferenceModel");
            XmlDocument xmlValidateCondicionInferenceModel = ReaderXml(pathxmlValidateCondicionInferenceModel);
            var ValidateCondicionInferenceModel = GetElementXmlDecision(xmlValidateCondicionInferenceModel, MinValue, codedoc, holdercode);
            Application decision;
            decision = JsonConvert.DeserializeObject<Application>(result);//Deserializa el JSON y lo mapea 
            if (Boolean.Parse(decision.message))
            {
                //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                //EL SIGUIENTE BLOQUE DESEARILIZA EL JSON DEVUELTO DEL WEBSERVICE, VALIDANDO SU CONTENIDO, Y APLICANDO LAS CONFIGURACIONES DE LOS XML
                //ADEMAS DE IR REALIZANDO LOS FILTROS CORRESPONDIENTE
                var DecisionModel = "";
                var i = 2;
                var uricontroller = InferenceModelField.GetPath("SelectModel");//Cargamos el Controlador para ejecutar la siguiente funcion
                var uricontroller2 = InferenceModelField.GetPath("ConsultSuper");
                var uricontroller3 = InferenceModelField.GetPath("Qualification");
                var cadena = "";
                var d = "";
                var user = HttpContext.User.Identity.Name;
                //Mostrara el resultado como una tabla html
                if (Supervision[0] == "Selection")
                {
                    d += "<div id='Modelo'>" +
                        "<div>" + decision.datetime + "</div>" +
                            "<table id='tabla' border='1' style='border-collapse: collapse; width: 100%;'>" +
                                "<tr>" +
                                    "<th align='center'>Tipo</th>" +
                                    "<th align='center'>Porc Confiabilidad</th>" +
                                    "<th align='center'>Fiabilidad</th>" +
                                    "<th align='center'>Confiabilidad</th>" +
                                    "<th align='center'>Opcion</th>" +
                                "</tr>";
                    if (FieldArray != null)
                    {
                        d += "<tr>";
                        foreach (XmlElement field in FieldArray)
                        {
                            if (field.GetAttribute("value").Equals(decision.decision.area))
                            {
                                if (!string.IsNullOrEmpty(field.GetAttribute("name")))
                                {
                                    d += "<td>" + field.GetAttribute("name") + "</td>";
                                    cadena += field.InnerText + ";" + field.GetAttribute("name") + ";";
                                    DecisionModel = field.InnerText + ";" + field.GetAttribute("name") + ";";
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(cadena))
                        {
                            d += "<td>" + decision.decision.area + "</td>";
                            cadena += decision.decision.area + ";" + decision.decision.area + ";";
                            DecisionModel = decision.decision.area + ";" + decision.decision.area + ";";
                        }
                        var boolConfiable = Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                        cadena += (decision.decision.porc_conf * 100).ToString("N1") + ";" + decision.decision.fiable + ";" + boolConfiable + ";" + 1 + ";";
                        d += "<td   class='" + decision.decision.area + "'><script>InferenceModel.GraficaSelection('" + decision.decision.area + "','" + (decision.decision.porc_conf * 100).ToString("N1") + "')</script></td>";
                        d += "<td align='center'>" + Fiable(decision.decision.fiable) + "</td>";
                        DecisionModel += decision.decision.porc_conf + ";" + decision.decision.fiable + ";" + boolConfiable + ";" + 1 + ";" + decision.datetime + ";" + user;
                        if (boolConfiable != null)
                        {
                            if (boolConfiable.Equals("true"))
                            {
                                d += "<td align='center'><img src='/Content/Images/approve.png'></td>";
                            }
                            else
                            {
                                d += "<td align='center'><img src='/Content/Images/error.png'></td>";
                            }
                        }
                        else
                        {
                            d += "<td align='center'><img src='/Content/Images/help.png'></td>";
                        }
                        if (confirmation != null)
                        {
                            d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectSuper(`" + cadena + "`,`" + uricontroller2 + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                           "</tr>";
                        }
                        else
                        {
                            d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectCategory(`" + cadena + "`,`" + uricontroller + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                           "</tr>";
                        }

                        cadena = "";
                        if (decision.others != null)
                        {
                            foreach (Others others in decision.others)
                            {
                                if (i <= maxvalue)
                                {
                                    d += "<tr>";
                                    foreach (XmlElement field in FieldArray)
                                    {
                                        if (field.GetAttribute("value").Equals(others.area))
                                        {
                                            if (!string.IsNullOrEmpty(field.GetAttribute("name")))
                                            {
                                                d += "<td>" + field.GetAttribute("name") + "</td>";
                                                cadena += field.InnerText + ";" + field.GetAttribute("name") + ";";
                                            }
                                        }
                                    }
                                    if (string.IsNullOrEmpty(cadena))
                                    {
                                        d += "<td>" + others.area + "</td>";
                                        cadena += others.area + ";" + others.area + ";";
                                    }
                                    boolConfiable = Confiable(ValidateCondicionInferenceModel, others.area);
                                    cadena += (others.porc_conf * 100).ToString("N1") + ";" + others.fiable + ";" + boolConfiable + ";" + i + ";";
                                    d += "<td   class='" + others.area + "'><script>InferenceModel.GraficaSelection('" + others.area + "','" + (others.porc_conf * 100).ToString("N1") + "')</script></td>";
                                    d += "<td align='center'>" + Fiable(others.fiable) + "</td>";
                                    if (boolConfiable != null)
                                    {
                                        if (boolConfiable.Equals("true"))
                                        {
                                            d += "<td align='center'><img src='/Content/Images/approve.png'></td>";
                                        }
                                        else
                                        {
                                            d += "<td align='center'><img src='/Content/Images/error.png'></td>";
                                        }
                                    }
                                    else
                                    {
                                        d += "<td align='center'><img src='/Content/Images/help.png'></td>";
                                    }
                                    if (confirmation != null)
                                    {
                                        d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectSuper(`" + cadena + "`,`" + uricontroller2 + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                                       "</tr>";
                                    }
                                    else
                                    {
                                        d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectCategory(`" + cadena + "`,`" + uricontroller + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                                       "</tr>";
                                    }

                                    cadena = "";
                                    i++;
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        var boolConfiable = Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                        cadena = decision.decision.area + ";" + decision.decision.area + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + decision.decision.fiable + ";" + boolConfiable + ";" + 1 + ";";
                        DecisionModel = decision.decision.area + ";" + decision.decision.area + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + decision.decision.fiable + ";" + boolConfiable + ";" + 1 + ";" + decision.datetime + ";" + user;
                        d += "<tr>" +
                                "<td>" + decision.decision.area + "</td>" +
                                "<td   class='" + decision.decision.area + "'><script>InferenceModel.GraficaSelection('" + decision.decision.area + "','" + (decision.decision.porc_conf * 100).ToString("N1") + "')</script></td>" +
                                "<td align='center'>" + Fiable(decision.decision.fiable) + "</td>";

                        if (boolConfiable != null)
                        {
                            if (boolConfiable.Equals("true"))
                            {
                                d += "<td align='center'><img src='/Content/Images/approve.png'></td>";
                            }
                            else
                            {
                                d += "<td align='center'><img src='/Content/Images/error.png'></td>";
                            }
                        }
                        else
                        {
                            d += "<td align='center'><img src='/Content/Images/help.png'></td>";
                        }
                        if (confirmation != null)
                        {
                            d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectSuper(`" + cadena + "`,`" + uricontroller2 + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                           "</tr>";
                        }
                        else
                        {
                            d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectCategory(`" + cadena + "`,`" + uricontroller + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                           "</tr>";
                        }

                        cadena = "";
                        if (decision.others != null)
                        {
                            foreach (Others others in decision.others)
                            {
                                if (i <= maxvalue)
                                {
                                    boolConfiable = Confiable(ValidateCondicionInferenceModel, others.area);
                                    cadena = others.area + ";" + (others.porc_conf * 100).ToString("N1") + ";" + others.fiable + ";" + boolConfiable + ";" + i + ";";
                                    d += "<tr>" +
                                            "<td>" + others.area + "</td>" +
                                            "<td   class='" + others.area + "'><script>InferenceModel.GraficaSelection('" + others.area + "','" + (others.porc_conf * 100).ToString("N1") + "')</script></td>" +
                                            "<td align='center'>" + Fiable(others.fiable) + "</td>";

                                    if (boolConfiable != null)
                                    {
                                        if (boolConfiable.Equals("true"))
                                        {
                                            d += "<td align='center'><img src='/Content/Images/approve.png'></td>";
                                        }
                                        else
                                        {
                                            d += "<td align='center'><img src='/Content/Images/error.png'></td>";
                                        }
                                    }
                                    else
                                    {
                                        d += "<td align='center'><img src='/Content/Images/help.png'></td>";
                                    }
                                    if (confirmation != null)
                                    {
                                        d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectSuper(`" + cadena + "`,`" + uricontroller2 + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                                       "</tr>";
                                    }
                                    else
                                    {
                                        d += "<td align='center'><input type='button' value='Seleccionar' onclick='javascript:InferenceModel.SelectCategory(`" + cadena + "`,`" + uricontroller + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`)'></td>" +
                                       "</tr>";
                                    }

                                    cadena = "";
                                    i++;
                                }
                            }
                        }
                        
                    }
                    d += "</table>" +
                       "</div>";
                    var Database = "";
                    var caseFolder = DocumentService.GetCaseFolder(codedoc);
                    Document document = new Document();
                    StaticContent staticContent = null;
                    staticContent = ContentService.GetContentByCode(codedoc);
                    int.TryParse(fieldId, out int id);
                    var ServiceAudit = new AuditService();
                    var Field = CustomFieldService.GetField(id);
                    var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
                    var Inference = DecisionModel.Split(';');
                    if (!string.IsNullOrEmpty(fieldvalue.Value))
                    {
                        var values = fieldvalue.Value.Split('ß');
                        if (values.Length > 1 && values.Length < 3)
                        {
                            for (var j = 0; j < values.Length; j++)
                            {
                                Database += values[j] + "ß";
                            }
                            Database += DecisionModel;
                            Guid.TryParse(values[0], out Guid Id);
                            Audit audit = ServiceAudit.GetAudit(Id);
                            audit.AreaInference = Inference[0];
                            audit.PorConfInference = Inference[2];
                            audit.FiableInference = Inference[3];
                            audit.ConfiableInference = Inference[4];
                            audit.PositionInference = Inference[5];
                            audit.FechaInference = Inference[6];
                            audit.AuthorInference = Inference[7];
                            var a = ServiceAudit.InsertOrUpdateAudit(audit);
                            if (caseFolder != null)
                            {
                                caseFolder.SetFieldValue(Field, Database);
                            }
                            else
                            {
                                document = DocumentService.GetDocument(codedoc);
                                document.SetFieldValue(Field, Database);
                            }
                        }
                    }
                }
                else if (Supervision[0] == "Qualification")
                {

                    var Porc_conf = String.Empty;
                    d += "<div id='Modelo'>" +
                        "<div>" + decision.datetime + "</div>" +
                        "<div>"+
                            "<table id='tabla' style='border-collapse: collapse; width: 50%;' align='left'>"+
                    "<tr align='left' style='height: 100px;'>";
                    if (FieldArray != null)
                    {
                        foreach (XmlElement field in FieldArray)
                        {
                            if (field.GetAttribute("value").Equals(decision.decision.area))
                            {
                                if (!string.IsNullOrEmpty(field.GetAttribute("name")))
                                {
                                    d += "<td>" + field.GetAttribute("name") + "</td>";
                                }
                            }
                        } 
                    }
                    else
                    {
                        d += "<td>" + decision.decision.area + "</td>";
                    }

                    d+= "<td class='Qualification_" + (MinValue.Replace(' ', '_')) + "'>" +
                            "<div class='bien_"+ (MinValue.Replace(' ', '_')) +"'></div>" +
                            "<div class='neutro_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div class='mal_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div class='izquierda_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div class='derecha_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div class='centro_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div class='rango_" + (MinValue.Replace(' ', '_')) + "'></div>" +
                            "<div><label class='flecha_" + (MinValue.Replace(' ', '_')) + "'>▲</label></div>" +
                        "</td>" +
                        "</tr>";
                    d += "<script>InferenceModel.GraficaQualification(" + decision.decision.porc_conf + ",20,60,'" + (MinValue.Replace(' ', '_')) + "')</script>";

                    var boolConfiable = Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                    DecisionModel = decision.decision.area + ";" + decision.decision.area + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + decision.decision.fiable + ";" + boolConfiable + ";" + 1 + ";" + decision.datetime + ";" + user;

                    d += "<tr>" +
                        "<td align='right'> <p>Le fue util?</p> </td>" +
                        "<td align='left'> " +
                            "<p class='clasificacion'>";
                    for(var j = 1; j < 6; j++)
                    {
                        if(qualification!=null)
                        {
                            if (int.Parse(qualification) == (6-j))
                            {
                                d += "<input id='radio" + j + "' type='radio' name='estrellas' value='" + (6 - j) + "' checked>" +
                                "<label for='radio" + j + "'>★</label>";
                            }
                            else
                            {
                                d += "<input id='radio" + j + "' type='radio' name='estrellas' value='" + (6 - j) + "'>" +
                                "<label for='radio" + j + "'>★</label>";
                            }
                        }
                        else
                        {
                            d += "<input id='radio" + j + "' type='radio' name='estrellas' onclick='javascript:InferenceModel.Qualification(this,`" + uricontroller3 + "`,`" + fieldname + "`,`" + codedoc + "`,`" + fieldId + "`,`" + MinValue + "`);' value='" + (6 - j) + "'>" +
                                "<label for='radio" + j + "'>★</label>";
                        }
                        
                    }
                    d+=
                            "</p>" +
                        "</td>" +
                        "</tr>" +
                        "</table>" +
                    "</div>" +
                    "</div>";
                    var Database = "";
                    var caseFolder = DocumentService.GetCaseFolder(codedoc);
                    Document document = new Document();
                    StaticContent staticContent = null;
                    staticContent = ContentService.GetContentByCode(codedoc);
                    int.TryParse(fieldId, out int id);
                    var ServiceAudit = new AuditService();
                    var Field = CustomFieldService.GetField(id);
                    var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
                    var Inference = DecisionModel.Split(';');
                    if (!string.IsNullOrEmpty(fieldvalue.Value))
                    {
                        var values = fieldvalue.Value.Split('ß');
                        if (values.Length > 1 && values.Length < 3)
                        {
                            for (var j = 0; j < values.Length; j++)
                            {
                                Database += values[j] + "ß";
                            }
                            Database += DecisionModel;
                            Guid.TryParse(values[0], out Guid Id);
                            Audit audit = ServiceAudit.GetAudit(Id);
                            audit.AreaInference = Inference[0];
                            audit.PorConfInference = Inference[2];
                            audit.FiableInference = Inference[3];
                            audit.ConfiableInference = Inference[4];
                            audit.PositionInference = Inference[5];
                            audit.FechaInference = Inference[6];
                            audit.AuthorInference = Inference[7];
                            var a = ServiceAudit.InsertOrUpdateAudit(audit);
                            if (caseFolder != null)
                            {
                                caseFolder.SetFieldValue(Field, Database);
                            }
                            else
                            {
                                document = DocumentService.GetDocument(codedoc);
                                document.SetFieldValue(Field, Database);
                            }
                        }
                    }
                }
                return d;
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------------------------------
        private string LoginSession(string url)
        {
            try
            {
                var Login = new Dictionary<string, string>();
                Login.Add("email", "b@b.com");
                Login.Add("password", "b");
                var request = (HttpWebRequest)WebRequest.Create(url + "API/Login/");
                request.Method = "POST";
                var jsonSerializer = new JavaScriptSerializer();
                jsonSerializer.MaxJsonLength = 500000000;
                string json = jsonSerializer.Serialize(Login);
                using (var writter = new StreamWriter(request.GetRequestStream()))
                {
                    writter.Write(json);
                    writter.Close();
                }
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = reader.ReadToEnd();
                    if (result != null)
                    {
                        Application decision;
                        decision = JsonConvert.DeserializeObject<Application>(result);
                        if (decision.message != null)
                        {
                            return decision.message;
                        }
                        return null;
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public ActionResult Qualification(string Qualification, string FieldId, string Code, string Fieldname, string Minvalue)
        {
            var script = "<script type=\"text/javascript\"> " +
                    "$(\".InferenceModelButton_" + Fieldname + "\").hide(); " +
                    "</script>";
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document document = new Document();
            StaticContent staticContent = null;
            staticContent = ContentService.GetContentByCode(Code);
            var fieldvalue = CustomFieldService.GetFieldValueContent(staticContent, Field.Code);
            var Database = "";
            var ServiceAudit = new AuditService();
            var values = fieldvalue.Value.Split('ß');
            if (values.Length > 2 && values.Length < 4)
            {
                for (var j = 0; j < values.Length; j++)
                {
                    Database += values[j] + "ß";
                }
                Database += Qualification;
            }
            Guid.TryParse(values[0], out Guid Id);
            Audit audit = ServiceAudit.GetAudit(Id);
            audit.Qualification = Qualification;
            var a = ServiceAudit.InsertOrUpdateAudit(audit);
            if (caseFolder != null)
            {
                caseFolder.SetFieldValue(Field, Database);
            }
            else
            {
                document = DocumentService.GetDocument(Code);
                document.SetFieldValue(Field, Database);
            }
            return Json(script+CreatedTable(Database.Split('ß')[1],null,Fieldname,Code,FieldId, 1, Minvalue,null, Database.Split('ß')[3]));
        }

        public ActionResult RemoveInference(string Code, string MinValue, string DefaultValue, string MaxValue, string Fieldname, string FieldId, string Holdercode)
        {
            int.TryParse(FieldId, out int id);
            var Field = CustomFieldService.GetField(id);
            var caseFolder = DocumentService.GetCaseFolder(Code);
            Document doc = new Document();
            if (caseFolder != null)
            {
               caseFolder.SetFieldValue(Field, string.Empty);
            }
            else
            {
                doc = DocumentService.GetDocument(Code);
                doc.SetFieldValue(Field, string.Empty);
            }
            return Json("");
        }

    }
}
