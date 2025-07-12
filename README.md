# WcfRestBridge

> A lightweight runtime bridge that exposes legacy WCF services as modern RESTful endpoints via .NET.

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
[![stars - wcfrestbridge](https://img.shields.io/github/stars/engineering87/WcfRestBridge?style=social)](https://github.com/engineering87/WcfRestBridge)
[![issues - wcfrestbridge](https://img.shields.io/github/issues/engineering87/WcfRestBridge)](https://github.com/engineering87/WcfRestBridge/issues)

## Overview

**WcfRestBridge** is a runtime adapter that allows you to invoke legacy WCF (SOAP) services through a modern RESTful API using .NET. This solution is particularly useful in migration scenarios or when maintaining interoperability between existing WCF-based backends and new frontend or client applications built with REST.

The bridge dynamically scans WCF service interfaces annotated with a custom attribute, exposes them via REST, and forwards requests to the corresponding SOAP endpoints at runtime.

## Features

- 🔁 **Dynamic WCF-to-REST mapping** via interface metadata
- 🧩 **Plug-and-play**: no code generation or proxy classes required
- 🧠 **Reflection-based SOAP invocation**
- 🛡️ **Swagger UI** for testing and exploration
- ⚙️ **Configurable endpoints** via `appsettings.json`
- ✅ Compatible with **.NET Standard**, **.NET Core**, and **.NET Framework (via service references)**

---

## Architecture

       +-------------+           +---------------------+
       | REST Client |  --->     |   ASP.NET Core API  |
       +-------------+           | (WcfRestBridge.Api) |
                                 +---------------------+
                                           |
                                           v
                                +----------------------+
                                |  WcfRestBridge.Core  |
                                | - Interface scanner  |
                                | - Dynamic invoker    |
                                +----------------------+
                                           |
                                           v
                       +-------------------------------------+
                       |  WcfRestBridge.WcfAttribute         |
                       |  (.NET Standard 2.0)                |
                       | - Shared attribute [WcfRestService] |
                       +-------------------------------------+
                                           |
                                           v
                                +--------------------------+
                                |     Legacy WCF           |
                                |   (SOAP endpoint)        |
                                | (WcfRestBridge.TestHost) |
                                +--------------------------+

## Projects

### `WcfRestBridge.Core`

Contains the core logic for:

- Discovering WCF interfaces via `[WcfRestService]` attribute
- Reflective SOAP invocation
- Managing route metadata

### `WcfRestBridge.WcfAttribute` (.NET Standard 2.0)

This is a shared library containing the `[WcfRestService]` attribute used to annotate WCF service interfaces.
Targeting .NET Standard 2.0 ensures compatibility between the legacy WCF project (.NET Framework) and the modern .NET API (.NET 6+).

### `WcfRestBridge.Api`

An ASP.NET Core Web API project that:

- Hosts the REST controller (`RestProxyController`)
- Uses Swagger for testing
- Loads WCF endpoint configuration
- Binds incoming JSON payloads and maps them to SOAP method calls

### `WcfRestBridge.TestHost`

A WCF Service Application (targeting .NET Framework 4.7.2) containing one or more interfaces and WCF implementations decorated with `[WcfRestService("YourPrefix")]`.

## Contributing
Thank you for considering to help out with the source code!
If you'd like to contribute, please fork, fix, commit and send a pull request for the maintainers to review and merge into the main code base.

 * [Setting up Git](https://docs.github.com/en/get-started/getting-started-with-git/set-up-git)
 * [Fork the repository](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo)
 * [Open an issue](https://github.com/engineering87/WcfRestBridge/issues) if you encounter a bug or have a suggestion for improvements/features

### Licensee
WcfRestBridge source code is available under MIT License, see license in the source.

### Contact
Please contact at francesco.delre[at]protonmail.com for any details.
