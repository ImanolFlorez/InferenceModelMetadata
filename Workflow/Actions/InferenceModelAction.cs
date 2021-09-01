using Adapting.Core.Contents;
using Adapting.Core.Metadata;
using Adapting.Core.Workflow;
using Adapting.Core.Workflow.Actions;
using Adapting.Document.Controllers;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Adapting.Document.Services;
using Newtonsoft.Json;
using InferenceModelMetadata.Json;
using System.Xml;
using System.Web;
using Adapting.Document.Domain;
using InferenceModelMetadata.Services;
using InferenceModelMetadata.Domain;

namespace InferenceModelMetadata.Workflow.Actions

{
    [Export(typeof(IWorkflowAction))]
    class InferenceModelAction : IWorkflowAction
    {

        private string ActionName = "InferenceModel";

        private IDocumentService documentService;
        private IDocumentService DocumentService { get { return this.documentService ?? (this.documentService = ServiceLocator.Current.GetInstance<IDocumentService>()); } }

        public IList<WorkflowArgumentInfo> GetArgumentsInfo()
        {
            throw new NotImplementedException();
        }

        public string GetDescription()
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }
        private ICustomFieldService customFieldService;
        private ICustomFieldService CustomFieldService { get { return this.customFieldService ?? (this.customFieldService = ServiceLocator.Current.GetInstance<ICustomFieldService>()); } }









        public WorkflowResult Validate(WorkflowAction workflowAction)
        {
            return ValidateAction(workflowAction.WorkflowParams);

        }
        public WorkflowResult ValidateAction(List<WorkflowActionParam> parameters)
        {
            WorkflowActionParam fieldCode1 = parameters.Where(x => x.Name.ToLower() == ActionArguments.fieldCode.ToLower()).FirstOrDefault();
            WorkflowActionParam minpercent = parameters.Where(x => x.Name.ToLower() == ActionArguments.MinPercent.ToLower()).FirstOrDefault();
            if (minpercent != null)
            {
                if (!Double.TryParse(minpercent.Value,out var number ))
                {
                    return new WorkflowResult(false, string.Format("La acción \"{0}\" requiere que parámetro \"{1}\" sea un numero.", ActionName, ActionArguments.MinPercent));
                }
            }
            if (fieldCode1 == null)
            {
                return new WorkflowResult(false, string.Format("La acción \"{0}\" requiere el parámetro \"{1}\".", ActionName, ActionArguments.fieldCode));
            }
            var field1 = CustomFieldService.GetField(fieldCode1.Value);
            if (field1 == null)
            {
                return new WorkflowResult(false, string.Format("Error en la acción \"{0}\": el valor del parámetro \"{1}\" no corresponde a un metadato.", ActionName, ActionArguments.fieldCode));
            } 
       
            return new WorkflowResult(true);
        }
        public WorkflowResult Execute(WorkflowContextBase context, IList<WorkflowActionParam> actionParams)
        {
            try
            {
                //Obtiene parametro
                var fieldCode = actionParams.Where(x => x.Name.ToLower() == ActionArguments.fieldCode.ToLower()).FirstOrDefault();
                var confianza = actionParams.Where(x => x.Name.ToLower() == ActionArguments.confianza.ToLower()).FirstOrDefault();
                var MinPercent = actionParams.Where(x => x.Name.ToLower() == ActionArguments.MinPercent.ToLower()).FirstOrDefault();
                var selectFirst = actionParams.Where(x => x.Name.ToLower() == ActionArguments.selectFirst.ToLower()).FirstOrDefault();
                var Holdercode = actionParams.Where(x => x.Name.ToLower() == ActionArguments.holdercode.ToLower()).FirstOrDefault();
                if (fieldCode == null)
                {
                    return new WorkflowResult(false, "(WorkflowActionParam) Los parametros Son Obligatorios");
                }
                if (confianza == null)
                {
                    confianza = new WorkflowActionParam();
                    confianza.Value = "NA";
                }
                if (MinPercent == null)
                {
                    MinPercent = new WorkflowActionParam();
                    MinPercent.Value = "NA";
                }
                if (selectFirst == null)
                {
                    selectFirst = new WorkflowActionParam();
                    selectFirst.Value = "NA";
                }
                if(Holdercode == null)
                {
                    Holdercode = new WorkflowActionParam();
                    Holdercode.Value = string.Empty;
                }
                //valida que sea un staticContent
                if (!(context?.Content is StaticContent))
                {
                    return new WorkflowResult(false, "No se ha especificado un contexto válido.");
                }
                var staticContent = context.Content as StaticContent;
                //obtiene el codigo del cotenido
                var Code = staticContent.Code;
                CaseFolder casefolder = null;
                casefolder = context.Content as CaseFolder;
                var Id = string.Empty;
                var DocumentCode = string.Empty;
                var DocumentId = string.Empty;
                var CaseFolderId = string.Empty;
                var CaseFolderCode = string.Empty;
                var Holder = Holdercode.Value;
                var fieldId = string.Empty;
                var FieldCode = fieldCode.Value;
                if (casefolder != null)
                {
                    CaseFolderCode = casefolder.Code;
                    CaseFolderId = casefolder.Id.ToString();
                    var holders = casefolder.CaseFolderHolders;
                    if (holders != null && holders.Any())
                    {
                        holders = holders.Where(x => x.CaseFolderElement.Code.Equals(Holdercode.Value)).OrderByDescending(x=> x.Document.DateModified).ToList();
                        if (holders.Any())
                        {
                            foreach (var holder in holders)
                            {
                                if (holder.Document.IsActive)
                                {
                                    Code = holder.Document.Code;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            return new WorkflowResult(false, "En la acción: " + ActionName + ", No se encontro el Holder especificado");
                        }
                    }
                }
                //llamado al servicio customFieldService
                var customFieldService = ServiceLocator.Current.GetInstance<ICustomFieldService>();
                //se obtiene el field
                var ServiceAudit = new AuditService();
                var field = customFieldService.GetField(fieldCode.Value);
                
                //crear un metodo que obtenga lo que viene el ws
                InferenceModelController inferenceModelController = new InferenceModelController();
                var pathxmlInferenceModelEquivalence = inferenceModelController.LoadXmlRouteInferenceModel("EquivalenceInferenceModelXml");
                XmlDocument xmlInferenceModelEquivalence = inferenceModelController.ReaderXml(pathxmlInferenceModelEquivalence);
                var pathxmlValidateCondicionInferenceModel = inferenceModelController.LoadXmlRouteInferenceModel("ValidateResultsInferenceModel");
                XmlDocument xmlValidateCondicionInferenceModel = inferenceModelController.ReaderXml(pathxmlValidateCondicionInferenceModel);
                var ValidateCondicionInferenceModel = inferenceModelController.GetElementXmlDecision(xmlValidateCondicionInferenceModel, field.MinValue, Code, Holdercode.Value);
                var FieldArray = inferenceModelController.GetElementXml(xmlInferenceModelEquivalence, field.MinValue, "Category");
                var TypeModel = inferenceModelController.GetTypeSupervision(xmlInferenceModelEquivalence, field.MinValue);
                var resultado = inferenceModelController.ConsultAction(Code, field.MinValue, field.DefaultValue, field.MaxValue, "Action", field.Id.ToString(), Holdercode.Value);
                var doc = DocumentService.GetDocument(Code);
                DocumentCode = doc.Code;
                DocumentId = doc.Id.ToString();
                fieldId = field.Id.ToString();
                if (resultado != null)
                {
                    var decision = JsonConvert.DeserializeObject<Application>(resultado);
                    if (bool.Parse(decision.message))
                    {
                        var min = !string.IsNullOrEmpty(MinPercent.Value) ? MinPercent.Value : "NA";
                        var conf = string.IsNullOrEmpty(confianza.Value) ? "NA" : confianza.Value.ToLower() != "true" ? "NA" : confianza.Value;
                        string name = string.Empty;
                        var now = decision.datetime;
                        var Automatico = "Automatico";
                        var user = HttpContext.Current.User.Identity.Name;
                        Audit AfterRegister;
                        
                        if (!string.IsNullOrEmpty(Holder))
                        {
                            AfterRegister = ServiceAudit.SelectRegisterIsCaseFolder(CaseFolderId, fieldId, Holder);
                        }
                        else
                        {
                            AfterRegister = ServiceAudit.SelectRegisterIsDocument(DocumentId, fieldId);
                        }
                        switch (TypeModel.Split(';')[0])
                        {
                            
                            case "Selection":
                                Audit audit = new Audit();
                                Audit a;
                                var booleanConfiable = "";
                                var selectfirt = "";
                                string[] Cadena;
                                if (selectFirst.Value.ToLower() == "true")
                                {
                                    if (AfterRegister != null)
                                    {
                                        ServiceAudit.Remove(AfterRegister);
                                    }
                                    audit = new Audit
                                    {
                                        DocumentCode = DocumentCode,
                                        DocumentId = DocumentId,
                                        FieldCode = FieldCode,
                                        FieldId = fieldId,
                                        CaseFolderCode = CaseFolderCode,
                                        CaseFolderId = CaseFolderId,
                                        HolderCode = Holder,
                                        InferenceModel = field.MinValue,
                                        TypeAction = "Automatico"
                                    };
                                    AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                    foreach (XmlElement array in FieldArray)
                                    {
                                        if (array.GetAttribute("value").Equals(decision.decision.area))
                                        {
                                            if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                            {
                                                name = array.InnerText + ";" + array.GetAttribute("name");
                                            }
                                        }
                                    }
                                    booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                                    selectfirt = name + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + decision.decision.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                    Cadena = selectfirt.Split(';');
                                    AfterRegister.AreaInference = Cadena[0];
                                    AfterRegister.PorConfInference = Cadena[2];
                                    AfterRegister.ConfiableInference = Cadena[3];
                                    AfterRegister.FiableInference = Cadena[4];
                                    AfterRegister.AuthorInference = Cadena[5];
                                    AfterRegister.FechaInference = Cadena[6];
                                    AfterRegister.PositionInference = "1";
                                    AfterRegister.AreaSelect = Cadena[0];
                                    AfterRegister.PorConfSelect = Cadena[2];
                                    AfterRegister.ConfiableSelect = Cadena[3];
                                    AfterRegister.FiableSelect = Cadena[4];
                                    AfterRegister.AuthorSelect = Cadena[5];
                                    AfterRegister.FechaSelect = Cadena[6];
                                    AfterRegister.PositionSelect = "1";
                                    AfterRegister.LogId = decision.logid;
                                    a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                    staticContent.SetFieldValue(field, selectfirt);
                                    return new WorkflowResult(true);
                                }
                                else
                                {
                                    foreach (XmlElement array in FieldArray)
                                    {
                                        if (array.GetAttribute("value").Equals(decision.decision.area))
                                        {
                                            if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                            {
                                                name = name = array.InnerText + ";" + array.GetAttribute("name");
                                            }
                                        }
                                    }
                                    booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                                    selectfirt = name + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + decision.decision.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                    var first = selectfirt.Split(';');
                                    Cadena = selectfirt.Split(';');
                                    if (min != "NA" && conf != "NA")
                                    {
                                        if (decision.decision.porc_conf >= double.Parse(min) && booleanConfiable.Equals("true"))
                                        {
                                            if (AfterRegister != null)
                                            {
                                                ServiceAudit.Remove(AfterRegister);
                                            }
                                            audit = new Audit
                                            {
                                                DocumentCode = DocumentCode,
                                                DocumentId = DocumentId,
                                                FieldCode = FieldCode,
                                                FieldId = fieldId,
                                                CaseFolderCode = CaseFolderCode,
                                                CaseFolderId = CaseFolderId,
                                                HolderCode = Holder,
                                                InferenceModel = field.MinValue,
                                                TypeAction = "Automatico"
                                            };
                                            AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                            AfterRegister.AreaInference = first[0];
                                            AfterRegister.PorConfInference = first[2];
                                            AfterRegister.ConfiableInference = first[3];
                                            AfterRegister.FiableInference = first[4];
                                            AfterRegister.AuthorInference = first[5];
                                            AfterRegister.FechaInference = first[6];
                                            AfterRegister.PositionInference = "1";
                                            AfterRegister.AreaSelect = Cadena[0];
                                            AfterRegister.PorConfSelect = Cadena[2];
                                            AfterRegister.ConfiableSelect = Cadena[3];
                                            AfterRegister.FiableSelect = Cadena[4];
                                            AfterRegister.AuthorSelect = Cadena[5];
                                            AfterRegister.FechaSelect = Cadena[6];
                                            AfterRegister.PositionSelect = "1";
                                            AfterRegister.LogId = decision.logid;
                                            a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                            staticContent.SetFieldValue(field, selectfirt);
                                            return new WorkflowResult(true);
                                        }
                                        else
                                        {
                                            var i = 2;
                                            foreach (Others item in decision.others)
                                            {
                                                booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, item.area);
                                                if (item.porc_conf >= double.Parse(min) && booleanConfiable.Equals("true"))
                                                {
                                                    foreach (XmlElement array in FieldArray)
                                                    {
                                                        if (array.GetAttribute("value").Equals(item.area))
                                                        {
                                                            if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                                            {
                                                                name = array.InnerText + ";" + array.GetAttribute("name");
                                                            }
                                                        }
                                                    }
                                                    if (AfterRegister != null)
                                                    {
                                                        ServiceAudit.Remove(AfterRegister);
                                                    }
                                                    audit = new Audit
                                                    {
                                                        DocumentCode = DocumentCode,
                                                        DocumentId = DocumentId,
                                                        FieldCode = FieldCode,
                                                        FieldId = fieldId,
                                                        CaseFolderCode = CaseFolderCode,
                                                        CaseFolderId = CaseFolderId,
                                                        HolderCode = Holder,
                                                        InferenceModel = field.MinValue,
                                                        TypeAction = "Automatico"
                                                    };
                                                    AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                                    var other = name + ";" + (item.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + item.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                                    Cadena = other.Split(';');
                                                    AfterRegister.AreaInference = first[0];
                                                    AfterRegister.PorConfInference = first[2];
                                                    AfterRegister.ConfiableInference = first[3];
                                                    AfterRegister.FiableInference = first[4];
                                                    AfterRegister.AuthorInference = first[5];
                                                    AfterRegister.FechaInference = first[6];
                                                    AfterRegister.PositionInference = "1";
                                                    AfterRegister.AreaSelect = Cadena[0];
                                                    AfterRegister.PorConfSelect = Cadena[2];
                                                    AfterRegister.ConfiableSelect = Cadena[3];
                                                    AfterRegister.FiableSelect = Cadena[4];
                                                    AfterRegister.AuthorSelect = Cadena[5];
                                                    AfterRegister.FechaSelect = Cadena[6];
                                                    AfterRegister.PositionSelect = i.ToString();
                                                    AfterRegister.LogId = decision.logid;
                                                    a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                                    staticContent.SetFieldValue(field, other);
                                                    return new WorkflowResult(true);
                                                }
                                                i++;
                                            }
                                        }
                                    }
                                    else if (min != "NA")
                                    {
                                        if (decision.decision.porc_conf >= double.Parse(min))
                                        {
                                            if (AfterRegister != null)
                                            {
                                                ServiceAudit.Remove(AfterRegister);
                                            }
                                            audit = new Audit
                                            {
                                                DocumentCode = DocumentCode,
                                                DocumentId = DocumentId,
                                                FieldCode = FieldCode,
                                                FieldId = fieldId,
                                                CaseFolderCode = CaseFolderCode,
                                                CaseFolderId = CaseFolderId,
                                                HolderCode = Holder,
                                                InferenceModel = field.MinValue,
                                                TypeAction = "Automatico"
                                            };
                                            AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                            AfterRegister.AreaInference = first[0];
                                            AfterRegister.PorConfInference = first[2];
                                            AfterRegister.ConfiableInference = first[3];
                                            AfterRegister.FiableInference = first[4];
                                            AfterRegister.AuthorInference = first[5];
                                            AfterRegister.FechaInference = first[6];
                                            AfterRegister.PositionInference = "1";
                                            AfterRegister.AreaSelect = Cadena[0];
                                            AfterRegister.PorConfSelect = Cadena[2];
                                            AfterRegister.ConfiableSelect = Cadena[3];
                                            AfterRegister.FiableSelect = Cadena[4];
                                            AfterRegister.AuthorSelect = Cadena[5];
                                            AfterRegister.FechaSelect = Cadena[6];
                                            AfterRegister.PositionSelect = "1";
                                            AfterRegister.LogId = decision.logid;
                                            a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                            staticContent.SetFieldValue(field, selectfirt);
                                            return new WorkflowResult(true);
                                        }
                                        else
                                        {
                                            var i = 2;
                                            foreach (var item in decision.others)
                                            {
                                                booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, item.area);
                                                if (item.porc_conf >= double.Parse(min))
                                                {
                                                    foreach (XmlElement array in FieldArray)
                                                    {
                                                        if (array.GetAttribute("value").Equals(item.area))
                                                        {
                                                            if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                                            {

                                                                name = array.InnerText + ";" + array.GetAttribute("name");
                                                            }
                                                        }
                                                    }
                                                    if (AfterRegister != null)
                                                    {
                                                        ServiceAudit.Remove(AfterRegister);
                                                    }
                                                    audit = new Audit
                                                    {
                                                        DocumentCode = DocumentCode,
                                                        DocumentId = DocumentId,
                                                        FieldCode = FieldCode,
                                                        FieldId = fieldId,
                                                        CaseFolderCode = CaseFolderCode,
                                                        CaseFolderId = CaseFolderId,
                                                        HolderCode = Holder,
                                                        InferenceModel = field.MinValue,
                                                        TypeAction = "Automatico"
                                                    };
                                                    AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                                    var other = name + ";" + (item.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + item.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                                    Cadena = other.Split(';');
                                                    AfterRegister.AreaInference = first[0];
                                                    AfterRegister.PorConfInference = first[2];
                                                    AfterRegister.ConfiableInference = first[3];
                                                    AfterRegister.FiableInference = first[4];
                                                    AfterRegister.AuthorInference = first[5];
                                                    AfterRegister.FechaInference = first[6];
                                                    AfterRegister.PositionInference = "1";
                                                    AfterRegister.AreaSelect = Cadena[0];
                                                    AfterRegister.PorConfSelect = Cadena[2];
                                                    AfterRegister.ConfiableSelect = Cadena[3];
                                                    AfterRegister.FiableSelect = Cadena[4];
                                                    AfterRegister.AuthorSelect = Cadena[5];
                                                    AfterRegister.FechaSelect = Cadena[6];
                                                    AfterRegister.PositionSelect = i.ToString();
                                                    AfterRegister.LogId = decision.logid;
                                                    a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                                    staticContent.SetFieldValue(field, other);
                                                    return new WorkflowResult(true);
                                                }
                                                i++;
                                            }
                                        }
                                    }
                                    else if (conf != "NA")
                                    {
                                        booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                                        if (booleanConfiable.Equals("true"))
                                        {
                                            if (AfterRegister != null)
                                            {
                                                ServiceAudit.Remove(AfterRegister);
                                            }
                                            audit = new Audit
                                            {
                                                DocumentCode = DocumentCode,
                                                DocumentId = DocumentId,
                                                FieldCode = FieldCode,
                                                FieldId = fieldId,
                                                CaseFolderCode = CaseFolderCode,
                                                CaseFolderId = CaseFolderId,
                                                HolderCode = Holder,
                                                InferenceModel = field.MinValue,
                                                TypeAction = "Automatico"
                                            };
                                            AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                            AfterRegister.AreaInference = first[0];
                                            AfterRegister.PorConfInference = first[2];
                                            AfterRegister.ConfiableInference = first[3];
                                            AfterRegister.FiableInference = first[4];
                                            AfterRegister.AuthorInference = first[5];
                                            AfterRegister.FechaInference = first[6];
                                            AfterRegister.PositionInference = "1";
                                            AfterRegister.AreaSelect = Cadena[0];
                                            AfterRegister.PorConfSelect = Cadena[2];
                                            AfterRegister.ConfiableSelect = Cadena[3];
                                            AfterRegister.FiableSelect = Cadena[4];
                                            AfterRegister.AuthorSelect = Cadena[5];
                                            AfterRegister.FechaSelect = Cadena[6];
                                            AfterRegister.PositionSelect = "1";
                                            AfterRegister.LogId = decision.logid;
                                            a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                            staticContent.SetFieldValue(field, selectfirt);
                                            return new WorkflowResult(true);
                                        }
                                        else
                                        {
                                            var i = 2;
                                            foreach (var item in decision.others)
                                            {
                                                booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, item.area);
                                                if (booleanConfiable.Equals("true"))
                                                {
                                                    foreach (XmlElement array in FieldArray)
                                                    {
                                                        if (array.GetAttribute("value").Equals(item.area))
                                                        {
                                                            if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                                            {
                                                                name = array.InnerText + ";" + array.GetAttribute("name");
                                                            }
                                                        }
                                                    }
                                                    if (AfterRegister != null)
                                                    {
                                                        ServiceAudit.Remove(AfterRegister);
                                                    }
                                                    audit = new Audit
                                                    {
                                                        DocumentCode = DocumentCode,
                                                        DocumentId = DocumentId,
                                                        FieldCode = FieldCode,
                                                        FieldId = fieldId,
                                                        CaseFolderCode = CaseFolderCode,
                                                        CaseFolderId = CaseFolderId,
                                                        HolderCode = Holder,
                                                        InferenceModel = field.MinValue,
                                                        TypeAction = "Automatico"
                                                    };
                                                    AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                                    var other = name + ";" + (item.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + item.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                                    Cadena = other.Split(';');
                                                    AfterRegister.AreaInference = first[0];
                                                    AfterRegister.PorConfInference = first[2];
                                                    AfterRegister.ConfiableInference = first[3];
                                                    AfterRegister.FiableInference = first[4];
                                                    AfterRegister.AuthorInference = first[5];
                                                    AfterRegister.FechaInference = first[6];
                                                    AfterRegister.PositionInference = "1";
                                                    AfterRegister.AreaSelect = Cadena[0];
                                                    AfterRegister.PorConfSelect = Cadena[2];
                                                    AfterRegister.ConfiableSelect = Cadena[3];
                                                    AfterRegister.FiableSelect = Cadena[4];
                                                    AfterRegister.AuthorSelect = Cadena[5];
                                                    AfterRegister.FechaSelect = Cadena[6];
                                                    AfterRegister.PositionSelect = i.ToString();
                                                    AfterRegister.LogId = decision.logid;
                                                    a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                                    staticContent.SetFieldValue(field, other);
                                                    return new WorkflowResult(true);
                                                }
                                                i++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        audit = new Audit
                                        {
                                            DocumentCode = DocumentCode,
                                            DocumentId = DocumentId,
                                            FieldCode = FieldCode,
                                            FieldId = fieldId,
                                            CaseFolderCode = CaseFolderCode,
                                            CaseFolderId = CaseFolderId,
                                            HolderCode = Holder,
                                            InferenceModel = field.MinValue,
                                            TypeAction = "Automatico"
                                        };
                                        AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                        foreach (XmlElement array in FieldArray)
                                        {
                                            if (array.GetAttribute("value").Equals(decision.decision.area))
                                            {
                                                if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                                {
                                                    name = array.InnerText + ";" + array.GetAttribute("name");
                                                }
                                            }
                                        }
                                        booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                                        selectfirt = name + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + decision.decision.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                        Cadena = selectfirt.Split(';');
                                        AfterRegister.AreaInference = Cadena[0];
                                        AfterRegister.PorConfInference = Cadena[2];
                                        AfterRegister.ConfiableInference = Cadena[3];
                                        AfterRegister.FiableInference = Cadena[4];
                                        AfterRegister.AuthorInference = Cadena[5];
                                        AfterRegister.FechaInference = Cadena[6];
                                        AfterRegister.PositionInference = "1";
                                        AfterRegister.LogId = decision.logid;
                                        a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                        staticContent.SetFieldValue(field, a.Id.ToString() + 'ß' + resultado + 'ß' + selectfirt);
                                        return new WorkflowResult(true);
                                        
                                    }
                                }
                                return new WorkflowResult(false, "En la acción: " + ActionName + ", No se pudo tomar una desicion");
                            case "Qualification":
                          
                                if (AfterRegister != null)
                                {
                                    ServiceAudit.Remove(AfterRegister);
                                }
                                audit = new Audit
                                {
                                    DocumentCode = DocumentCode,
                                    DocumentId = DocumentId,
                                    FieldCode = FieldCode,
                                    FieldId = fieldId,
                                    CaseFolderCode = CaseFolderCode,
                                    CaseFolderId = CaseFolderId,
                                    HolderCode = Holder,
                                    InferenceModel = field.MinValue,
                                    TypeAction = "Automatico"
                                };
                                AfterRegister = ServiceAudit.InsertOrUpdateAudit(audit);
                                foreach (XmlElement array in FieldArray)
                                {
                                    if (array.GetAttribute("value").Equals(decision.decision.area))
                                    {
                                        if (!string.IsNullOrEmpty(array.GetAttribute("name")))
                                        {
                                            name = array.InnerText + ";" + array.GetAttribute("name");
                                        }
                                    }
                                }
                                booleanConfiable = inferenceModelController.Confiable(ValidateCondicionInferenceModel, decision.decision.area);
                                selectfirt = name + ";" + (decision.decision.porc_conf * 100).ToString("N1") + ";" + booleanConfiable + ";" + decision.decision.fiable + ";" + user + ";" + now + ";" + Automatico + ";" + decision.logid;
                                Cadena = selectfirt.Split(';');
                                AfterRegister.AreaInference = Cadena[0];
                                AfterRegister.PorConfInference = Cadena[2];
                                AfterRegister.ConfiableInference = Cadena[3];
                                AfterRegister.FiableInference = Cadena[4];
                                AfterRegister.AuthorInference = Cadena[5];
                                AfterRegister.FechaInference = Cadena[6];
                                AfterRegister.PositionInference = "1";
                                AfterRegister.LogId = decision.logid;
                                a = ServiceAudit.InsertOrUpdateAudit(AfterRegister);
                                staticContent.SetFieldValue(field, a.Id.ToString()+ 'ß' + resultado + 'ß' + selectfirt);
                                return new WorkflowResult(true);       
                            default:
                                return new WorkflowResult(false, "En la acción: " + ActionName + ", No se encontro el modelo de tipo " + TypeModel.Split(';')[0] + " Verifique su archivo de configuracion");
                        }     
                    }
                    return new WorkflowResult(false, "En la acción: " + ActionName + ", No se pudo tomar una desicion");
                }
                else
                {
                    return new WorkflowResult(false, "En la acción: " + ActionName + ", Se produjo un error en la llamada del servicio web");
                }
            }
            catch (Exception e)
            {
                return new WorkflowResult(false, "En la acción: " + ActionName + ", " + e.Message);
            }
        }

        internal class ActionArguments
        {
            public const string holdercode = "holdercode";
            public const string fieldCode = "fieldCode";
            public const string selectFirst = "selectFirst";
            public const string MinPercent = "minPercent";
            public const string confianza = "confiable";
        }
    }
}
