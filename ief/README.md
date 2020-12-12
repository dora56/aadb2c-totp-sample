# Custom Policy

## Prerequisites

- VSCode and [Azure AD B2C extension](https://marketplace.visualstudio.com/items?itemName=AzureADB2CTools.aadb2c) already installed.

## Get Start

#### 1. Please set the following environment variables in appsettings.json.

``` json
{
    "CustomSelfassertedUrl": "your uploaded content/selfasserted-appfactor-registration.html by blob url",
    "GenerateAPI": "qrcode genareate api url",
    "VelfiyAPI": "velify code api url",
    "ApplicationObjectId": "your dev b2c-extensions-app ObjectId",
    "ClientId": "your dev b2c-extensions-app Application Id",
    "IdentityExperienceFrameworkAppId": "Your dev environment AD app Id",
    "ProxyIdentityExperienceFrameworkAppId": "Your AD dev environment Proxy app Id"
}
```
#### 2. You execute the [B2C Policy build command](https://github.com/azure-ad-b2c/vscode-extension#policy-settings).

`Ctrl+Shift+P B2C Ctrl+Shift+5`

#### 3. upload your custom policy. 
