// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    public struct SolutionCriteriaValue
    {
        public string uniquename { get; set; }
    }

    public struct SolutionCriteria
    {
        public SolutionCriteriaValue criteria { get; set; }
    }

    public struct SolutionApplyObj
    {
        public string action { get; set; }
        public List<SolutionCriteria> solutions { get; set; }
    }

    public struct SolutionRequestResponse
    {
        public string requestId { get; set; }
    }


    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public struct SolutionResultMessage
    {
        public string message { get; set; }
        public string status { get; set; }
    }

    public struct SolutionResult
    {
        public string requestId { get; set; }
        public string action { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public string state { get; set; }
        public SolutionResultMessage result { get; set; }
        public string rootActivityId { get; set; }
    }




}
