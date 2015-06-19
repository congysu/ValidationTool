﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ODataValidator.Rule
{
    #region Namespaces
    using System;
    using System.ComponentModel.Composition;
    using System.Data.Metadata.Edm;
    using System.Linq;
    using System.Web;
    using System.Xml.Linq;
    using ODataValidator.Rule.Helper;
    using ODataValidator.RuleEngine;
    using Newtonsoft.Json.Linq;
    #endregion

    /// <summary>
    /// Class of entension rule for rule #303
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class EntryCore2013 : ExtensionRule
    {
        /// <summary>
        /// Gets Categpry property
        /// </summary>
        public override string Category
        {
            get
            {
                return "core";
            }
        }

        /// <summary>
        /// Gets rule name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Entry.Core.2013";
            }
        }

        /// <summary>
        /// Gets rule description
        /// </summary>
        public override string Description
        {
            get
            {
                return @"A NavigationProperty which is serialized inline MUST be represented as a name/value pair on the JSON object with the name equal to the NavigationProperty name.";
            }
        }

        /// <summary>
        /// Gets rule specification in OData document
        /// </summary>
        public override string SpecificationSection
        {
            get
            {
                return "2.2.6.3.9.1";
            }
        }

        /// <summary>
        /// Gets location of help information of the rule
        /// </summary>
        public override string HelpLink
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the error message for validation failure
        /// </summary>
        public override string ErrorMessage
        {
            get
            {
                return this.Description;
            }
        }

        /// <summary>
        /// Gets the requriement level.
        /// </summary>
        public override RequirementLevel RequirementLevel
        {
            get
            {
                return RequirementLevel.Must;
            }
        }

        /// <summary>
        /// Gets the payload type to which the rule applies.
        /// </summary>
        public override PayloadType? PayloadType
        {
            get
            {
                return RuleEngine.PayloadType.Entry;
            }
        }

        /// <summary>
        /// Gets the payload format to which the rule applies.
        /// </summary>
        public override PayloadFormat? PayloadFormat
        {
            get
            {
                return RuleEngine.PayloadFormat.Json;
            }
        }

        /// <summary>
        /// Gets the flag whether it applies to offline context.
        /// </summary>
        public override bool? IsOfflineContext
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Verify the rule
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="info">out paramater to return violation information when rule fail</param>
        /// <returns>true if rule passes; false otherwise</returns>
        public override bool? Verify(ServiceContext context, out ExtensionRuleViolationInfo info)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool? passed = null;
            info = null;

            // get the stack of navigation properties of expand query option
            var qs = HttpUtility.ParseQueryString(context.Destination.Query);
            string qExpand = qs["$expand"];
            if (!string.IsNullOrEmpty(qExpand))
            {
                RuleEngine.TestResult result = null;
                JObject jo = JObject.Parse(context.ResponsePayload);
                var version = JsonParserHelper.GetPayloadODataVersion(jo);

                var edmxHelper = new EdmxHelper(XElement.Parse(context.MetadataDocument));
                EntityType et;
                edmxHelper.TryGetItem(context.EntityTypeFullName, out et);

                var branches = ResourcePathHelper.GetBranchedSegments(qExpand);
                foreach(var paths in branches)
                {
                    var navStack = ODataUriAnalyzer.GetNavigationStack(et, paths).ToArray();
                    bool[] targetIsCollection = (from n in navStack select n.RelationshipMultiplicity == RelationshipMultiplicity.Many).ToArray();

                    for (int i = 0; i < paths.Length; i++)
                    {
                        string jsCore = string.Format(EntryCore2013.fmtCore, paths[i]);
                        string jSchema = JsonSchemaHelper.GetJsonSchema(paths, i, version, jsCore, targetIsCollection);

                        passed = JsonParserHelper.ValidateJson(jSchema, context.ResponsePayload, out result);
                        if (!passed.Value)
                        {
                            break;
                        }
                    }
                }

                if (!passed.Value)
                {
                    info = new ExtensionRuleViolationInfo(this.ErrorMessage, context.Destination, context.ResponsePayload, result != null ? result.LineNumberInError : -1);
                }
            }

            return passed;
        }

        private const string fmtCore = @"""{0}"" : {{""type"":""any"", ""required"" : true }}";
    }
}

