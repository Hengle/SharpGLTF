﻿using SharpGLTF.Validation;
using System.Collections.Generic;
namespace SharpGLTF.Schema2
{
    public partial class MeshExtInstanceFeatures
    {
        private Node _node;
        internal MeshExtInstanceFeatures(Node node)
        {
            _node = node;
            _featureIds = new List<MeshExtInstanceFeatureID>();
        }

        public List<MeshExtInstanceFeatureID> FeatureIds
        {
            get
            {
                return _featureIds;
            }
            set
            {
                _featureIds = value;
            }
        }

        protected override void OnValidateReferences(ValidationContext validate)
        {
            var extInstanceFeatures = _node.GetExtension<MeshExtInstanceFeatures>();
            validate.NotNull(nameof(extInstanceFeatures), extInstanceFeatures);
            var extMeshGpInstancing = _node.GetExtension<MeshGpuInstancing>();
            validate.NotNull(nameof(extMeshGpInstancing), extMeshGpInstancing);

            foreach (var instanceFeatureId in FeatureIds)
            {
                if (instanceFeatureId.Attribute.HasValue)
                {
                    var expectedVertexAttribute = $"_FEATURE_ID_{instanceFeatureId.Attribute}";
                    var gpuInstancing = _node.GetGpuInstancing();
                    var featureIdAccessors = gpuInstancing.GetAccessor(expectedVertexAttribute);
                    Guard.NotNull(featureIdAccessors, expectedVertexAttribute);
                }

                if (instanceFeatureId.PropertyTable.HasValue)
                {
                    var metadataExtension = _node.LogicalParent.GetExtension<EXTStructuralMetadataRoot>();
                    Guard.NotNull(metadataExtension, nameof(metadataExtension), "EXT_Structural_Metadata extension is not found.");
                    Guard.NotNull(metadataExtension.PropertyTables[instanceFeatureId.PropertyTable.Value], nameof(instanceFeatureId.PropertyTable), $"Property table index {instanceFeatureId.PropertyTable.Value} does not exist");
                }
            }

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            var extInstanceFeatures = _node.GetExtension<MeshExtInstanceFeatures>();
            validate.NotNull(nameof(FeatureIds), extInstanceFeatures.FeatureIds);
            validate.IsTrue(nameof(FeatureIds), extInstanceFeatures.FeatureIds.Count > 0, "Instance FeatureIds has items");


            foreach (var instanceFeatureId in FeatureIds)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)instanceFeatureId.FeatureCount, 1, nameof(instanceFeatureId.FeatureCount));

                if (instanceFeatureId.NullFeatureId.HasValue)
                {
                    Guard.MustBeGreaterThanOrEqualTo((int)instanceFeatureId.NullFeatureId, 0, nameof(instanceFeatureId.NullFeatureId));
                }
                if (instanceFeatureId.Label != null)
                {
                    var regex = "^[a-zA-Z_][a-zA-Z0-9_]*$";
                    Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(instanceFeatureId.Label, regex), nameof(instanceFeatureId.Label));
                }

                if (instanceFeatureId.Attribute.HasValue)
                {
                    Guard.MustBeGreaterThanOrEqualTo((int)instanceFeatureId.Attribute, 0, nameof(instanceFeatureId.Attribute));
                }
                if (instanceFeatureId.PropertyTable.HasValue)
                {
                    Guard.MustBeGreaterThanOrEqualTo((int)instanceFeatureId.PropertyTable, 0, nameof(instanceFeatureId.PropertyTable));
                }
            }

            base.OnValidateContent(validate);
        }
    }

    public partial class MeshExtInstanceFeatureID
    {
        public MeshExtInstanceFeatureID()
        {
        }

        public MeshExtInstanceFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null, int? nullFeatureId = null)
        {
            _featureCount = featureCount;
            _attribute = attribute;
            _label = label;
            _propertyTable = propertyTable;
            _nullFeatureId = nullFeatureId;
        }

        /// <summary>
        /// The number of unique features in the attribute
        /// </summary>
        public int FeatureCount { get => _featureCount; }

        /// <summary>
        /// An attribute containing feature IDs. When this is omitted, then the feature IDs are assigned to the GPU instances by their index.
        /// </summary>
        public int? Attribute { get => _attribute; }

        /// <summary>
        /// A label assigned to this feature ID set
        /// </summary>
        public string Label { get => _label; }

        /// <summary>
        /// The index of the property table containing per-feature property values. Only applicable when using the `EXT_structural_metadata` extension.
        /// </summary>
        public int? PropertyTable { get => _propertyTable; }

        /// <summary>
        /// A value that indicates that no feature is associated with this instance
        /// </summary>
        public int? NullFeatureId { get => _nullFeatureId; }
    }

    public static class ExtInstanceFeatures
    {
        /// <summary>
        /// Set the instance feature ids for this node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="instanceFeatureIds"></param>
        public static void SetFeatureIds(this Node node, List<MeshExtInstanceFeatureID> instanceFeatureIds)
        {
            if (instanceFeatureIds == null) { node.RemoveExtensions<MeshExtInstanceFeatures>(); return; }

            Guard.NotNullOrEmpty(instanceFeatureIds, nameof(instanceFeatureIds));

            var extMeshGpInstancing = node.GetExtension<MeshGpuInstancing>();
            Guard.NotNull(extMeshGpInstancing, nameof(extMeshGpInstancing));

            var ext = node.UseExtension<MeshExtInstanceFeatures>();
            ext.FeatureIds = instanceFeatureIds;
        }
    }
}