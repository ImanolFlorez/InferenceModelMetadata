using Adapting.Core.Contents;
using Adapting.Core.Metadata;
using Adapting.Core.Workflow;
using Adapting.Core.Workflow.Guards;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InferenceModelMetadata.Workflow.Guards
{
    [Export(typeof(IWorkflowGuard))]
    class InferenceModelConfirmationGuard : IWorkflowGuard
    {
        private string MessageToShow = "messageToShow";
        public WorkflowResult Execute(WorkflowContextBase context, IList<WorkflowGuardParam> guardParams)
        {
            const string guardName = "InferenceModelConfirmation";
            var mes =
                          string.Format(
                              "El contenido no puede cambiar de estado porque no supero la condición. Guarda \"{0}\".",
                              guardName);

            WorkflowGuardParam mts =
                guardParams.FirstOrDefault(x => x.Name.ToLower() == "messagetoshow");

            if (mts != null)
            {
                mes = "<strong>" + mts.Value + "</strong> (" + mes + ").";
            }
            var validationResult = ValidateGuard((List<WorkflowGuardParam>)guardParams, guardName);
            if (validationResult.IsSuccess)
            {
                var staticContent = context.Content as StaticContent;

                if (staticContent != null)
                {

                    var fieldcode = guardParams.FirstOrDefault(x => x.Name.ToLower() == "fieldcode");
                    var fieldValue = staticContent.GetFieldValue(fieldcode.Value);
                    if (fieldValue != null)
                    {
                        if(fieldValue.Value != null)
                        {
                            var split = fieldValue.Value.Split('ß');
                            if (split.Length > 1)
                            {
                                return new WorkflowResult(false, mes);
                            }
                            var resul = split[0];

                            var resplit = resul.Split(';');
                            if (resplit.Length <= 1)
                            {
                                return new WorkflowResult(false, mes);
                            }
                            return new WorkflowResult(true);
                        }
                        
                    }

                }

            }
            return new WorkflowResult(false, mes);
        }

        public IList<WorkflowArgumentInfo> GetArgumentsInfo()
        {
            throw new NotImplementedException();
        }

        public string GetDescription()
        {
            return "Revisa el metadato inference model para comprobar que ya a sido confirmado ";
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public WorkflowResult Validate(WorkflowGuard workflowGuard)
        {
            List<WorkflowGuardParam> parameters = workflowGuard.WorkflowGuardParams;
            return ValidateGuard(parameters, workflowGuard.Name);
        }
        private WorkflowResult ValidateGuard(List<WorkflowGuardParam> parameters, string guardName)
        {
            if (parameters.Count < 2)
            {
                return new WorkflowResult(false, string.Format("La guarda \"{0}\" debe tener 1 parámetros:\"fieldcode\" ", guardName));
            }
            if (parameters[0].Name.ToLower() != "fieldcode")
            {
                return new WorkflowResult(false, string.Format("Error en la guarda \"{0}\": El parámetro debe ser \"fieldcode\"", guardName));
            }
            var customFieldService = ServiceLocator.Current.GetInstance<ICustomFieldService>();
            if (parameters[0].Name == "fieldcode")
            {
                Field field = customFieldService.GetField(parameters[0].Value);
                if (field == null)
                {
                    return new WorkflowResult(false, string.Format("Error en la guarda \"{0}\": El valor del  parámetro no corresponde al código de un metadato.", guardName));
                }
                if (field.Type != "InferenceModel")
                {
                    return new WorkflowResult(false, string.Format("Error en la guarda \"{0}\": El valor del  parámetro no corresponde al de un metadato tipo \"InferenceModel\".", guardName));
                }
            }
            return new WorkflowResult(true);
        }
    }
}
