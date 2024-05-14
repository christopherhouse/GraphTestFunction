# Graph Test Function
This repo contains a simple C# Azure Function that runs on a configurable timer trigger.  When the timer interval elapses, the Function attempts to run
an MS Graph API query.  The results of that request, including the body, status
code and hashed results are stored in an Azure Table Storage table for future
analysis.

# Deployment
This function was built with .Net 6 LTS.  When deploying, ensure the following
environment variables are set on your Function resource:

| Variable Name | Description |
|---------------|-------------|
| AzureWebJobsStorage | The connection string to the Azure Storage account used by the Functions run time and to store graph API request results |
| TenantId       | The Azure AD tenant ID that the Function will authenticate against |
| ClientId       | The Azure AD client ID that the Function will authenticate as |
| ClientSecret   | The Azure AD client secret that the Function will authenticate with |
| CronExpression | The cron expression that defines the Function's schedule |
| Scopes | A list of OAuth scopes for the graph auth token.  Multiple scopes can be provided, use a comma to delimit them.  e.g. `User.Read,Mail.Read` |
| GraphQueryUri | The URI of the Graph API query to run.  This should be a full URI, including the query parameters.  e.g. `https://graph.microsoft.com/v1.0/me/messages?top=100` |