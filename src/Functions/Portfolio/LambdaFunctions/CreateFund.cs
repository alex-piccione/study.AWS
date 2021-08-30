using System;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Jil;
using Learning.Exceptions;

namespace Learning.Portfolio {
    class CreateFund : FundLambdaFunctionBase
    {
        private int CODE_MAX_LENGTH = 10;

        public CreateFund()
        { }

        public CreateFund(IFundRepository repository) : base(repository)
        { }


        /*
         function handler(event, context, callback) {
              const timer = setTimeout(() => {
                console.log("oh no i'm going to timeout in 3 seconds!");
                // &c.
              }, context.getRemainingTimeInMillis() - 3 * 1000);
              try {
                // rest of code...
              } finally {
                clearTimeout(timer);
              }
              callback(null, result);
            }
         */

        // TODO: use a timer to handle the Lambda function timeout
        private void HandleTimeout(ILambdaContext context) {
            var time = context.RemainingTime;
        }

        public override APIGatewayProxyResponse Handle(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                var requestData = NormalizeRequest(GetRequest<CreateFundRequest>(request.Body));

                ValidateRequest(requestData);

                var id = Guid.NewGuid().ToString();
                var fund = new Fund(id, requestData.Name, requestData.Code);

                repository.Create(fund);

                return CreateResponse(
                    HttpStatusCode.Created,
                    new CreateFundResponse
                    {
                        Id = fund.Id,
                        Name = fund.Name,
                        Code = fund.Code
                    }
                );
            }
            catch (DeserializationFailedException exc)
            {
                context.Logger.LogLine($"JSON:/n{exc.Json}/n/n{exc}");
                return CreateResponse(HttpStatusCode.BadRequest, $"Failed to deserialize. JSON: {exc.Json}");
            }
            catch (DeserializationException exc)
            {
                context.Logger.LogLine(exc.ToString());
                return CreateResponse(HttpStatusCode.BadRequest, "Cannot deserialize request body");
            }
            catch (InvalidRequestDataException exc)
            {
                context.Logger.LogLine(exc.ToString());
                return CreateResponse(HttpStatusCode.BadRequest, exc.Message) ;
            }
            catch (Exception exc)
            {
                context.Logger.LogLine(exc.ToString());
                return CreateErrorResponse(exc.Message);
            }
        }

        /// Trim all the string properties and set the code uppercase
        private CreateFundRequest NormalizeRequest(CreateFundRequest request)
        {
            request.Code = request.Code?.Trim().ToUpperInvariant();
            request.Name = request.Name?.Trim();
            return request;
        }

        private void ValidateRequest(CreateFundRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidRequestDataException("Name", "must not be empty");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new InvalidRequestDataException("Code", "must not be empty");
            else if (request.Code.Length > CODE_MAX_LENGTH)
                throw new Exception($"Code is too long. The max allowed length is {CODE_MAX_LENGTH}.");
        }
    }
}