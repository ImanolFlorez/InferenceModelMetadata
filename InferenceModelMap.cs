using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;

namespace WebserviceMetadata
{
    class InferenceModelMap : SubclassMap<InferenceModelField>
    {
        public InferenceModelMap() 
        {
            DiscriminatorValue("InferenceModel");
        }
    }
}
