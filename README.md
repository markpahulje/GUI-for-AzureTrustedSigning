# GUI for AzureTrustedSigning

**Graphical User Interface for Microsoft Azure Trusted Signing** to code sign your local application, ported to Net 4.8 Framework from [GUI-for-ATS](https://github.com/codenia/GUI-for-ATS)

## 📌 Overview
**GUI for ATS** is a user-friendly graphical interface that simplifies the use of **Microsoft Azure Trusted Signing**. It streamlines the process of signing files, making it easier for developers and businesses to integrate secure code signing into their workflow.

## 🔐 Security Advantage
The main advantage of this application is that you do not need to create the environment variables  
**`AZURE_CLIENT_ID`**, **`AZURE_TENANT_ID`**, and **`AZURE_CLIENT_SECRET`**.  
This data is securely stored in encrypted form after you enter it and is only used in plaintext during the signing process.

![GUI Screenshot](Screenshot%20GUI%20for%20ATS.jpg)

## 🔧 Requirements
- Windows 10 / 11
- Microsoft Visual Studio 2019+ to create the application
- .NET 4.8 Runtime
- Azure Trusted Signing account: https://learn.microsoft.com/en-us/azure/trusted-signing/quickstart

## 🚀 Usage
1. **Download the source code and start the project with Visual Studio**.
2. **Enter your own random values for PasswordHash, SaltKey and VIKey in the Helper.cs file**.
3. **Create the application**.
4. **Launch the application**.
5. **Enter your Azure Trusted Signing credentials**.
6. **Select all necessary files**.
7. **Select the file you want to sign**.
8. **Start the signing process**.

## 📝 License
This project is licensed under the [MIT License](LICENSE).


