using System;
using System.Collections.Generic;
using System.Text;
using Adapting.Core.Attributes;
using Adapting.Core.Contents;
using Adapting.Core.Metadata;
using Microsoft.Practices.ServiceLocation;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Adapting.Document.Services;
using Adapting.Core.Membership;
using System.Web;
using Adapting.Core.Web.Settings;
using Adapting.Core.Web.Extensions;
using Adapting.Document.Controllers;
using System.Xml;
using System.Linq;

namespace WebserviceMetadata
{
    [FieldImpl("InferenceModel", Description = "Campo de tipo webservice donde se ingresa la URL del servicio a consumir.")]
    public class InferenceModelField : Field
    {
        private IRepositoryService repositoryService;
        private IRepositoryService RepositoryService{get{return this.repositoryService ??(this.repositoryService = ServiceLocator.Current.GetInstance<IRepositoryService>());}}

        private IDocumentService documentService;
        private IDocumentService DocumentService{get{return this.documentService ??(this.documentService = ServiceLocator.Current.GetInstance<IDocumentService>());}}
        
        private IUserService userService;
        private IUserService UserService{get{return this.userService ??(this.userService = ServiceLocator.Current.GetInstance<IUserService>());}}
        
        private ICustomFieldService customFieldService;
        private ICustomFieldService CustomFieldService{get{return this.customFieldService ??(this.customFieldService = ServiceLocator.Current.GetInstance<ICustomFieldService>());}}

        public override string Type{get { return "InferenceModel"; }}

        public override IDictionary<string, string> ValidateDefinition()
        {
            var result = new Dictionary<string, string>();

            // Validar que se suministre una URL.
            if (String.IsNullOrEmpty(DefaultValue))
            {
                result.Add("DefaultValue", "Se debe introducir una URL para realizar la predicción.");
            }
            else
            {
                // Crea y valida una URI a partir de la URL indicada en el metadato.
                Uri uriResult;
                bool valid = Uri.TryCreate(DefaultValue, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!valid)
                {
                    result.Add("DefaultValue", "Se debe definir una URL con esquema HTTP o HTTPS valida para realizar la predicción.");
                }
            }

            // Validación número máximo de resultados
            if (String.IsNullOrEmpty(MaxValue))
            {
                result.Add("MaxValue", "Se debe definir unas numero max de categorias que desee que modelo sugiera.");
            }
            else
            {
                var maxValue = 0;
                if (!int.TryParse(MaxValue, out maxValue))
                {
                    result.Add("MaxValue", "Por favor ingrese un número.");
                }
                else if (int.Parse(MaxValue) <= 0)
                {
                    result.Add("MaxValue", "Ingrese un numero mayor de 0");
                }
            }
            // Validación nombre del modelo
            if (String.IsNullOrEmpty(MinValue))
            {
                result.Add("MinValue", "Se debe introducir el nombre del modelo que desea llamar.");
            }
            else
            {
                InferenceModelController inferenceModelController = new InferenceModelController();
                //Obtiene el fichero de configuracion
                var pathInferenceModelConfig = inferenceModelController.LoadXmlRouteInferenceModel("InferenceModelXml");
                //Obtiene el XML parseable
                XmlDocument xmlInferenceModelConfig = inferenceModelController.ReaderXml(pathInferenceModelConfig);
                string[] Array = InferenceModelController.GetModels(xmlInferenceModelConfig);
                //Valida que exista el modelo existe en el xml de configuracion
                var a = Array.Any(x => x == MinValue);
                if (!a)
                {
                    result.Add("MinValue", "El Modelo Ingresado no existe.");
                }
            }

            return result;
        }

        public override string GetFieldRawValue(string value, StaticContent content)
        {
            var result = string.Empty;
            var Value = base.GetFieldRawValue(value, content);
            if (!string.IsNullOrEmpty(Value))
            {
                var split = Value.Split('ß');
                switch (split.Length)
                {
                    case 1:
                        if(split[0].Contains("http"))
                        {
                            return string.Empty;
                        }
                        else
                        {
                            result = split[0];
                        }
                        break;
                    case 3:
                        result = split[2];
                        break;
                    case 4:
                        result = split[3];
                        break;
                }
                if (!string.IsNullOrEmpty(result))
                {
                    var resplit = result.Split(';');
                    result = resplit[0];
                }
            }
            return result;
        }

        public override string ValidateValue(string attemptedValue)
        {
            if (this.Compulsory && string.IsNullOrEmpty(attemptedValue))
            {
                return "El campo es obligatorio";
            }

            return string.Empty;
        }

        public override object Clone()
        {
            var copyWebserviceField = new InferenceModelField();
            return CloneValues(copyWebserviceField);
        }

        public override MvcHtmlString DrawHtml(HtmlHelper helper, Field field, FieldValue fieldValue, StaticContent content)
        {
            var sb = new StringBuilder();
            var fieldName = string.Format("field{0}_{1}", field.Id, field.Schema.Id);
            var scripts = string.Empty;
            InferenceModelController inferenceModelController = new InferenceModelController();
            var pathInferenceModelConfig = inferenceModelController.LoadXmlRouteInferenceModel("InferenceModelXml");//Obtiene el fichero de configuracion
            XmlDocument xmlInferenceModelConfig = inferenceModelController.ReaderXml(pathInferenceModelConfig);//Obtiene el XML parseable
            string[] Array = InferenceModelController.GetModels(xmlInferenceModelConfig);
            var a = Array.Any(x => x == MinValue);
            if (!a)
            {
                return MvcHtmlString.Create("El Modelo Ingresado no existe.");
            }
            if (content != null)
            {
                string urlController1 = GetPath("FirstCall");
                string urlController2 = GetPath("Consultbutton");
                string urlController3 = GetPath("RemoveInference");
                var urlhelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

                scripts += helper.AddScript(urlhelper.Content("~/Content/Javascript/adapting.common.js"));
                scripts += helper.AddScript(urlhelper.Content("~/Content/Javascript/adapting.extensions.fields.InferenceModel.js"));
                scripts += "<script type=\"text/javascript\"> " +
                           "var InferenceModel = adapting.extensions.fields.InferenceModel;" +
                           "</script>";
                var holderCode = "";
                var defaultValue = DefaultValue;
                if (!string.IsNullOrEmpty(DefaultValue))
                {
                    var split = DefaultValue.Split('|');
                    if (split.Length > 1)
                    {
                        defaultValue = split[0];
                        holderCode = split[1];
                        //FolderService.GetContentByCode
                    }
                }
                scripts += "<script type=\"text/javascript\">" +
                    "$(document).ready(function (){" +
                    "InferenceModel.First('" + urlController1 + "','" + fieldName + "','" + content.Code + "','" + MinValue + "','" + defaultValue + "','" + MaxValue + "','" + Id + "','" + holderCode + "')" +
                    "});" +
                    "</script>";

                sb.Append("<input type=\"button\" class=\"InferenceModelButton_" + fieldName + "\" value=\"Consultar\" onclick=\"javascript:InferenceModel.ButtonCall('" + urlController2 + "','" + fieldName + "','" + content.Code + "','" + MinValue + "','" + defaultValue + "','" + MaxValue + "','" + Id + "','" + holderCode + "')\"> ");
                sb.Append("<input type=\"button\" class=\"InferenceModelRemove_" + fieldName + "\" value=\"Borrar\" onclick=\"javascript:InferenceModel.ButtonRemove('" + urlController3 + "','" + fieldName + "','" + content.Code + "','" + MinValue + "','" + defaultValue + "','" + MaxValue + "','" + Id + "','" + holderCode + "')\"> ");
                sb.Append("<div class='" + fieldName + "'></div>");

            }
            return MvcHtmlString.Create(sb.ToString() + scripts);
        }


        public override MvcHtmlString ViewValue(HtmlHelper helper, string value, StaticContent content)
        {
            var fieldName = string.Format("field{0}_{1}", Id, Schema.Id);
            //Verificamos URL
            if (string.IsNullOrEmpty(DefaultValue))
            {
                return MvcHtmlString.Create("No ha agregado una URL valida para consumir el servicio web.");
            }
            if (String.IsNullOrEmpty(MaxValue))
            {
                return MvcHtmlString.Create("Se debe definir unas numero max de categorias que desee que modelo sugiera.");
            }
            else
            {
                var maxValue = 0;
                if (!int.TryParse(MaxValue, out maxValue))
                {
                    return MvcHtmlString.Create("Por favor ingrese un número.");
                }
                else if (int.Parse(MaxValue) <= 0)
                {
                    return MvcHtmlString.Create("Ingrese un numero mayor de 0");
                }
            }
            if (String.IsNullOrEmpty(MinValue))
            {
                return MvcHtmlString.Create("Se debe introducir el nombre del modelo que desea llamar.");
            }
            else
            {
                InferenceModelController inferenceModelController = new InferenceModelController();
                var pathInferenceModelConfig = inferenceModelController.LoadXmlRouteInferenceModel("InferenceModelXml");//Obtiene el fichero de configuracion
                XmlDocument xmlInferenceModelConfig = inferenceModelController.ReaderXml(pathInferenceModelConfig);//Obtiene el XML parseable
                string[] Array = InferenceModelController.GetModels(xmlInferenceModelConfig);
                var a = Array.Any(x => x == MinValue);
                if (!a)
                {
                    return MvcHtmlString.Create("El Modelo Ingresado no existe.");
                }

            }

            var scripts = string.Empty;
            var sb = new StringBuilder();
            if (content != null) 
            {
                string urlController1 = GetPath("FirstCall");
                string urlController2 = GetPath("Consultbutton");
                var urlhelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
             
                scripts += helper.AddScript(urlhelper.Content("~/Content/Javascript/adapting.common.js"));
                scripts += helper.AddScript(urlhelper.Content("~/Content/Javascript/adapting.extensions.fields.InferenceModel.js"));
                scripts += "<script type=\"text/javascript\"> " +
                           "var InferenceModel = adapting.extensions.fields.InferenceModel;"+
                           "</script>";     
                var holderCode = "";
                var defaultValue = DefaultValue;
                if (!string.IsNullOrEmpty(DefaultValue))
                {
                    var split = DefaultValue.Split('|');
                    if (split.Length > 1)
                    {
                        defaultValue = split[0];
                        holderCode = split[1];
                        //FolderService.GetContentByCode
                    }
                }
                scripts += "<script type=\"text/javascript\">" +
                    "$(document).ready(function (){" +
                    "InferenceModel.First('" + urlController1 + "','" + fieldName + "','" + content.Code + "','" + MinValue + "','" + defaultValue + "','" + MaxValue + "','" + Id + "','" + holderCode + "')" +
                    "});" +
                    "</script>";
               
                //sb.Append("<input type=\"button\" class=\"InferenceModelButton_" + fieldName + "\" value=\"Consultar\" onclick=\"javascript:InferenceModel.ButtonCall('" + urlController2 + "','" + fieldName + "','" + content.Code + "','" + MinValue + "','" + defaultValue + "','" + MaxValue + "','" + Id + "','" + holderCode + "')\"> ");
                sb.Append("<div class='" + fieldName + "'></div>"); 

            }

            return new MvcHtmlString(sb.ToString()+ scripts);

        }
        public static string GetPosition(string value, int pos)
        {
            return value.Split('|').Length > pos ? value.Split('|')[pos] : "";
        }

        public static string GetPath(string name)
        {
            var uri = HttpContext.Current.Request.Url;
            var url = uri.AbsoluteUri;

            string protocol = url.Split('/')[0];
            string host = url.Split('/')[2];
            string path = url.Split('/')[3];

            string siteViewMode = GlobalSettings.Current.SiteViewMode;
            string showPath = siteViewMode.ToLower() == "domain" ? "/Document/InferenceModel/" + name : string.Format("/{0}/Document/InferenceModel/" + name + "", path);

            return string.Format("{0}//{1}{2}", protocol, host, showPath);
        } 
        public static string fieldname(Field field)
        {
            return string.Format("field{0}_{1}", field.Id, field.Schema.Id);
        }
    }
}
