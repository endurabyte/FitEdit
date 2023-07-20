# dotnet tool install -g Amazon.Lambda.Tools
# dotnet tool update -g Amazon.Lambda.Tools
dotnet lambda deploy-function Dauer-Lambda-Cognito-SignUp
# dotnet lambda invoke-function Dauer-Lambda-Cognito-SignUp --payload "json goes here"
