﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiResponseTypeProviderTest
    {
        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresent()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController.Get));
            actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new[]
            {
                new ProducesResponseTypeAttribute(201),
                new ProducesResponseTypeAttribute(404),
            });

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
                result.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(typeof(BaseModel), responseType.Type);
                    Assert.False(responseType.IsDefaultResponse);
                    Assert.Collection(
                        responseType.ApiResponseFormats,
                        format => Assert.Equal("application/json", format.MediaType));
                },
                responseType =>
                {
                    Assert.Equal(301, responseType.StatusCode);
                    Assert.Equal(typeof(void), responseType.Type);
                    Assert.False(responseType.IsDefaultResponse);
                    Assert.Empty(responseType.ApiResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(404, responseType.StatusCode);
                    Assert.Equal(typeof(void), responseType.Type);
                    Assert.False(responseType.IsDefaultResponse);
                    Assert.Empty(responseType.ApiResponseFormats);
                });
        }

        [ApiConventionType(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController : ControllerBase
        {
            [Produces(typeof(BaseModel))]
            [ProducesResponseType(301)]
            [ProducesResponseType(404)]
            public Task<ActionResult<BaseModel>> Get(int id) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromApiConventionItem()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));

            actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new[]
            {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(400),
                new ProducesResponseTypeAttribute(404),
            });

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(200, responseType.StatusCode);
                   Assert.Equal(typeof(BaseModel), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Collection(
                        responseType.ApiResponseFormats,
                        format => Assert.Equal("application/json", format.MediaType));
               },
               responseType =>
               {
                   Assert.Equal(400, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               },
               responseType =>
               {
                   Assert.Equal(404, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               });
        }

        [ApiConventionType(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> DeleteBase(int id) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatch()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController),
                nameof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController.PostModel));

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(200, responseType.StatusCode);
                   Assert.Equal(typeof(BaseModel), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Collection(
                        responseType.ApiResponseFormats,
                        format => Assert.Equal("application/json", format.MediaType));
               });
        }

        [ApiConventionType(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> PostModel(int id, BaseModel model) => null;
        }

        private static ApiResponseTypeProvider GetProvider()
        {
            var mvcOptions = new MvcOptions
            {
                OutputFormatters = { new TestOutputFormatter() },
            };
            var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), mvcOptions);
            return provider;
        }

        private static ControllerActionDescriptor GetControllerActionDescriptor(Type type, string name)
        {
            var method = type.GetMethod(name);
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method,
                FilterDescriptors = new List<FilterDescriptor>(),
            };

            foreach (var filterAttribute in method.GetCustomAttributes().OfType<IFilterMetadata>())
            {
                actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(filterAttribute, FilterScope.Action));
            }

            return actionDescriptor;
        }

        public class BaseModel { }

        public class DerivedModel : BaseModel { }

        public class TestController
        {
            public ActionResult<DerivedModel> GetUser(int id) => null;

            public ActionResult<DerivedModel> GetUserLocation(int a, int b) => null;

            public ActionResult<DerivedModel> PutModel(string userId, DerivedModel model) => null;
        }

        private class TestOutputFormatter : OutputFormatter
        {
            public TestOutputFormatter()
            {
                SupportedMediaTypes.Add(new Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context) => Task.CompletedTask;
        }

        public static class SearchApiConventions
        {
            [ProducesResponseType(206)]
            [ProducesResponseType(406)]
            public static void Search(object searchTerm, int page) { }
        }
    }
}