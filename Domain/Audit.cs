using FluentNHibernate;
using NHibernate.Validator.Constraints;
using System.Collections.Generic;

using System.Linq;
using Adapting.Common;
using System;
using Adapting.Common.Interfaces;
using Adapting.Common.Validation;
using Microsoft.Practices.ServiceLocation;

namespace InferenceModelMetadata.Domain

{

    public class Audit : PersistentObjectWithTypedId<Guid>
    {
       
        public virtual string DocumentId { get; set; }
        public virtual string DocumentCode { get; set; }
        public virtual string CaseFolderId { get; set; }
        public virtual string CaseFolderCode { get; set; }
        public virtual string FieldId { get; set; }
        public virtual string FieldCode { get; set; }
        public virtual string HolderCode { get; set; }
        public virtual string InferenceModel { get; set; }
        
        public virtual string AreaInference { get; set; }
        public virtual string PorConfInference { get; set; }
        public virtual string ConfiableInference { get; set; }
        public virtual string FiableInference { get; set; }
        public virtual string FechaInference { get; set; }
        public virtual string AuthorInference { get; set; }
        public virtual string PositionInference { get; set; }

        public virtual string AreaSelect { get; set; }
        public virtual string PorConfSelect { get; set; }
        public virtual string ConfiableSelect { get; set; }
        public virtual string FiableSelect { get; set; }
        public virtual string FechaSelect { get; set; }
        public virtual string AuthorSelect { get; set; }
        public virtual string PositionSelect { get; set; }

        public virtual string ApproveSuper { get; set; }
        public virtual string AreaSuper { get; set; }
        public virtual string PorConfSuper { get; set; }
        public virtual string ConfiableSuper { get; set; }
        public virtual string FiableSuper { get; set; }
        public virtual string FechaSuper { get; set; }
        public virtual string AuthorSuper { get; set; }
        public virtual string PositionSuper { get; set; }

        public virtual string TypeAction { get; set; }
        public virtual string LogId { get; set; }
        public virtual string Qualification { get; set; }
    }

}
