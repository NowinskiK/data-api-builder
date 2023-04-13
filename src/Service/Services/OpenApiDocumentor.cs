// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Azure.DataApiBuilder.Auth;
using Azure.DataApiBuilder.Service.Exceptions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Azure.DataApiBuilder.Service.Services
{
    public class OpenApiDocumentor : IOpenApiDocumentor
    {
        private ISqlMetadataProvider _metadataProvider;
        private IAuthorizationResolver _authorizationResolver;
        private OpenApiDocument? _openApiDocument;

        public OpenApiDocumentor(ISqlMetadataProvider sqlMetadataProvider, IAuthorizationResolver authorizationResolver)
        {
            _metadataProvider = sqlMetadataProvider;
            _authorizationResolver = authorizationResolver;
            _openApiDocument = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public bool TryGetDocument([NotNullWhen(true)] out string? document)
        {
            if (_openApiDocument is null)
            {
                document = null;
                return false;
            }

            using (StringWriter textWriter = new(CultureInfo.InvariantCulture))
            {
                OpenApiJsonWriter jsonWriter = new(textWriter);
                _openApiDocument.SerializeAsV3(jsonWriter);

                string jsonPayload = textWriter.ToString();
                document = jsonPayload;
                return true;
            }
        }

        public void CreateDocument()
        {
            if (_openApiDocument is not null)
            {
                throw new DataApiBuilderException(
                    message: "already created",
                    statusCode: System.Net.HttpStatusCode.Conflict,
                    subStatusCode: DataApiBuilderException.SubStatusCodes.OpenApiDocumentAlreadyExists);
            }

            OpenApiDocument doc = new()
            {
                Info = new OpenApiInfo
                {
                    Version = "0.0.1",
                    Title = "Data API builder - OpenAPI Description Document",
                },
                Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = "https://localhost:5000/api/openapi" }
                },
                Paths = new OpenApiPaths
                {
                    ["/Book/id/{id}"] = new OpenApiPathItem
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Get] = new OpenApiOperation
                            {
                                Description = "Returns all pets from the system that the user has access to",
                                Responses = new OpenApiResponses
                                {
                                    ["200"] = new OpenApiResponse
                                    {
                                        Description = "OK",
                                        Content = new Dictionary<string, OpenApiMediaType>()
                                        {
                                            {
                                                "application/json",
                                                new OpenApiMediaType()
                                                {
                                                    Schema = new OpenApiSchema()
                                                    {
                                                        Properties = new Dictionary<string, OpenApiSchema>()
                                                        {
                                                            {
                                                                "value",
                                                                new OpenApiSchema()
                                                                {
                                                                    Type = "array",
                                                                    Items = new OpenApiSchema()
                                                                    {
                                                                        Reference = new OpenApiReference()
                                                                        {
                                                                            Type = ReferenceType.Schema,
                                                                            Id = "BookResponse"
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>()
                        {
                            new OpenApiParameter()
                            {
                                Required = true,
                                In = ParameterLocation.Path,
                                Name = "id",
                                Schema = new OpenApiSchema()
                                {
                                    Type = "integer",
                                    Format = "int32"
                                }
                            }
                        }
                    }
                },
                Components = new OpenApiComponents()
                {
                    Schemas = new Dictionary<string, OpenApiSchema>()
                    {
                        {
                            "BookResponse",
                            new OpenApiSchema()
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>()
                                {
                                    {
                                        "id",
                                        new OpenApiSchema()
                                        {
                                            Type = "integer",
                                            Format = "int32"
                                        }
                                    },
                                    {
                                        "title",
                                        new OpenApiSchema()
                                        {
                                            Type = "string"
                                        }
                                    },
                                    {
                                        "publisher_id",
                                        new OpenApiSchema()
                                        {
                                            Type = "integer",
                                            Format = "int32"
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            };
            _openApiDocument = doc;
        }
    }
}