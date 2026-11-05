# Security Policy

## Supported Versions

This repository contains legacy .NET Framework sample applications that serve as a
baseline for modernization. There are no versioned releases; only the `main` branch
receives security fixes.

| Component | Framework | Supported |
| --- | --- | --- |
| `main` – eShopLegacyMVCSolution (ASP.NET MVC 5) | .NET Framework 4.7.2 | :white_check_mark: |
| `main` – eShopLegacyWebFormsSolution (ASP.NET Web Forms) | .NET Framework 4.7.2 | :white_check_mark: |
| `main` – eShopLegacyNTier (WCF + WinForms) | .NET Framework 4.6.1 / 4.7 | :white_check_mark: |
| Any other branch, fork, or historical commit | – | :x: |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues, pull
requests, or discussions.**

Report vulnerabilities privately using GitHub's private vulnerability reporting:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability** (or open
   <https://github.com/Cognition-Partner-Workshops/eShopModernizing/security/advisories/new>).
3. Include the affected application/project, the file(s) or endpoint involved,
   reproduction steps or a proof of concept, and the potential impact.

If private reporting is unavailable to you, contact the repository code owners listed in
[`.github/CODEOWNERS`](.github/CODEOWNERS) and ask for a private channel — do not include
vulnerability details in a public place.

## What to Expect

- Acknowledgement of your report within 5 business days.
- An initial assessment and severity rating within 10 business days.
- Fixes are developed in a private security advisory and merged to `main`; a GitHub
  Security Advisory is published once a fix is available, crediting the reporter unless
  anonymity is requested.

## Scope

In scope: code, configuration (e.g. `Web.config`), and NuGet dependencies in this
repository. Vulnerable third-party packages are also tracked by Dependabot
(`.github/dependabot.yml`). Out of scope: issues in upstream frameworks themselves —
report those to the relevant vendor (e.g. Microsoft Security Response Center).
