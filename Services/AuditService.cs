using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adapting.Common.Data.NH;
using Adapting.Common.Data;
using InferenceModelMetadata.Domain;

namespace InferenceModelMetadata.Services
{
    public class AuditService : IAuditService
    {

        private IRepository<Audit> AuditRepository { get; set; }
        public void Initialize()
        {
        }
        public AuditService()
        {
            AuditRepository = new NHibernateBaseRepository<Audit>();
        }
        public Audit InsertOrUpdateAudit(Audit audit)
        {var result= AuditRepository.SaveOrUpdate(audit);
            return result;
        }
        
        public Audit GetAudit(Guid Id)
        {
            return  AuditRepository.Get(Id);
        }
        public Audit SelectRegisterIsDocument(string Document, string Field)
        {
            return AuditRepository.FindOne(x => x.DocumentId == Document && x.FieldId == Field);    
        }
        public Audit SelectRegisterIsCaseFolder(string CaseFolder, string Field,string HolderCode)
        {
            return AuditRepository.FindOne(x => x.DocumentId == CaseFolder && x.FieldId == Field && x.HolderCode == HolderCode);
        }
        public void Remove(Audit audit)
        {
            AuditRepository.Delete(audit);
        }

        public IEnumerable<Audit> SelectForInferenceModel(string InferenceModel)
        {
            return AuditRepository.FindAll().Where(x => x.InferenceModel == InferenceModel);
        }
        public IEnumerable<Audit> SelectForTypeAction(string Action)
        {
            return AuditRepository.FindAll().Where(x => x.TypeAction == Action);
        }
        public IEnumerable<Audit> SelectForSupervision(string Supervision)
        {
            return AuditRepository.FindAll().Where(x => x.ApproveSuper == Supervision);
        }
        public IEnumerable<Audit> SelectForDatetime(string DateStart, string DateFinish)
        {
            return AuditRepository.FindAll().Where(x => Convert.ToDateTime(x.FechaInference) >= Convert.ToDateTime(DateStart) && Convert.ToDateTime(x.FechaInference) <= Convert.ToDateTime(DateFinish));
        }
        
        public IEnumerable<Audit> SelectMoreFilters(string InferenceModel = null, string Action=null, string Supervision=null, string DateStart=null, string DateFinish=null)
        {
            if (InferenceModel!=null && Action!=null)
            {

            }
            return AuditRepository.FindAll().Where(x => Convert.ToDateTime(x.FechaInference) >= Convert.ToDateTime(DateStart) && Convert.ToDateTime(x.FechaInference) <= Convert.ToDateTime(DateFinish));
        }
        public IEnumerable<Audit> FindAllAuditInfernceLog()
        {
            return AuditRepository.FindAll();
        }

    }
}
