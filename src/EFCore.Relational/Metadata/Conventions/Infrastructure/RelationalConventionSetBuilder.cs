// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         A service on the EF internal service provider that creates the <see cref="ConventionSet" />
    ///         for the current relational database provider. This is combined with <see cref="IConventionSetCustomizer" />
    ///         instances to produce the full convention set exposed by the <see cref="IConventionSetBuilder" />
    ///         service.
    ///     </para>
    ///     <para>
    ///         Database providers should implement this service by inheriting from either
    ///         this class (for relational providers) or <see cref="ProviderConventionSetBuilder" /> (for non-relational providers).
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
    ///         <see cref="DbContext" /> instance will use its own instance of this service.
    ///         The implementation may depend on other services registered with any lifetime.
    ///         The implementation does not need to be thread-safe.
    ///     </para>
    /// </summary>
    public abstract class RelationalConventionSetBuilder : ProviderConventionSetBuilder
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RelationalConventionSetBuilder" /> class.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
        /// <param name="relationalDependencies"> Parameter object containing relational dependencies for this service. </param>
        protected RelationalConventionSetBuilder(
            [NotNull] ProviderConventionSetBuilderDependencies dependencies,
            [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
            : base(dependencies)
        {
            Check.NotNull(relationalDependencies, nameof(relationalDependencies));

            RelationalDependencies = relationalDependencies;
        }

        /// <summary>
        ///     Parameter object containing relational service dependencies.
        /// </summary>
        protected virtual RelationalConventionSetBuilderDependencies RelationalDependencies { get; }

        /// <summary>
        ///     Builds and returns the convention set for the current database provider.
        /// </summary>
        /// <returns> The convention set for the current database provider. </returns>
        public override ConventionSet CreateConventionSet()
        {
            var conventionSet = base.CreateConventionSet();

            ValueGeneratorConvention valueGeneratorConvention = new RelationalValueGeneratorConvention(Dependencies, RelationalDependencies);

            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, valueGeneratorConvention);
            ReplaceConvention(conventionSet.EntityTypePrimaryKeyChangedConventions, valueGeneratorConvention);
            ReplaceConvention(conventionSet.ForeignKeyAddedConventions, valueGeneratorConvention);
            ReplaceConvention(conventionSet.ForeignKeyRemovedConventions, valueGeneratorConvention);

            var relationalColumnAttributeConvention = new RelationalColumnAttributeConvention(Dependencies, RelationalDependencies);

            conventionSet.PropertyAddedConventions.Add(relationalColumnAttributeConvention);

            var storeGenerationConvention = new StoreGenerationConvention(Dependencies, RelationalDependencies);
            conventionSet.EntityTypeAddedConventions.Add(new RelationalTableAttributeConvention(Dependencies, RelationalDependencies));
            conventionSet.EntityTypeBaseTypeChangedConventions.Add(new TableNameFromDbSetConvention(Dependencies, RelationalDependencies));
            conventionSet.PropertyFieldChangedConventions.Add(relationalColumnAttributeConvention);
            conventionSet.PropertyAnnotationChangedConventions.Add(storeGenerationConvention);
            conventionSet.PropertyAnnotationChangedConventions.Add((RelationalValueGeneratorConvention)valueGeneratorConvention);

            var sharedTableConvention = new SharedTableConvention(Dependencies, RelationalDependencies);
            ConventionSet.AddBefore(
                conventionSet.ModelFinalizedConventions,
                storeGenerationConvention,
                typeof(ValidatingConvention));
            ConventionSet.AddBefore(
                conventionSet.ModelFinalizedConventions,
                sharedTableConvention,
                typeof(ValidatingConvention));

            conventionSet.ModelAnnotationChangedConventions.Add(new RelationalDbFunctionConvention(Dependencies, RelationalDependencies));

            return conventionSet;
        }
    }
}