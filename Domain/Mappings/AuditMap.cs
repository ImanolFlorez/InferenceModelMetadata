using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;

namespace InferenceModelMetadata.Domain.Mappings
{
    public class AuditMap : ClassMap<Audit>
    {
        public AuditMap()
        {
            Table("Audit");
            Id(x => x.Id).GeneratedBy.GuidComb().Column("Id");
            Map(x => x.DocumentId).Column("DocumentId");
            Map(x => x.DocumentCode).Column("DocumentCode");
            Map(x => x.CaseFolderId).Column("CaseFolderId");
            Map(x => x.CaseFolderCode).Column("CaseFolderCode");
            Map(x => x.FieldId).Column("FieldId");
            Map(x => x.FieldCode).Column("FieldCode");
            Map(x => x.HolderCode).Column("HolderCode");
            Map(x => x.InferenceModel).Column("InferenceModel");
            Map(x => x.AreaInference).Column("AreaInference");
            Map(x => x.PorConfInference).Column("PorConfInference");
            Map(x => x.ConfiableInference).Column("ConfiableInference");
            Map(x => x.FiableInference).Column("FiableInference");
            Map(x => x.FechaInference).Column("FechaInference");
            Map(x => x.AuthorInference).Column("AuthorInference");
            Map(x => x.PositionInference).Column("PositionInference");
            Map(x => x.AreaSelect).Column("AreaSelect");
            Map(x => x.PorConfSelect).Column("PorConfSelect");
            Map(x => x.ConfiableSelect).Column("ConfiableSelect");
            Map(x => x.FiableSelect).Column("FiableSelect");
            Map(x => x.FechaSelect).Column("FechaSelect");
            Map(x => x.AuthorSelect).Column("AuthorSelect");
            Map(x => x.PositionSelect).Column("PositionSelect");
            Map(x => x.ApproveSuper).Column("ApproveSuper");
            Map(x => x.AreaSuper).Column("AreaSuper");
            Map(x => x.PorConfSuper).Column("PorConfSuper");
            Map(x => x.ConfiableSuper).Column("ConfiableSuper");
            Map(x => x.FiableSuper).Column("FiableSuper");
            Map(x => x.FechaSuper).Column("FechaSuper");
            Map(x => x.AuthorSuper).Column("AuthorSuper");
            Map(x => x.PositionSuper).Column("PositionSuper");
            Map(x => x.TypeAction).Column("TypeAction");
            Map(x => x.LogId).Column("LogId");
            Map(x => x.Qualification).Column("Qualification");
        }
    }  
}
