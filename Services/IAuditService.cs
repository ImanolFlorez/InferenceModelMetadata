using Adapting.Common.Interfaces;
using InferenceModelMetadata.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InferenceModelMetadata.Services
{
    public interface IAuditService : IService
    {
        
        
        Audit GetAudit(Guid Id);
        void Remove(Audit audit);
        Audit InsertOrUpdateAudit(Audit audit);
        IEnumerable<Audit> FindAllAuditInfernceLog();
        IEnumerable<Audit> SelectForTypeAction(string Action);
        IEnumerable<Audit> SelectForSupervision(string Supervision);
        Audit SelectRegisterIsDocument(string Document, string Field);
        IEnumerable<Audit> SelectForInferenceModel(string InferenceModel);
        IEnumerable<Audit> SelectForDatetime(string DateStart, string DateFinish);
        Audit SelectRegisterIsCaseFolder(string CaseFolder, string Field, string HolderCode);
        IEnumerable<Audit> SelectMoreFilters(string InferenceModel = null, string Action = null, string Supervision = null, string DateStart = null, string DateFinish = null);
        
      
    }
}
