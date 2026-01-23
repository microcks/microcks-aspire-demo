# Step 1: Getting Started

Before getting started, let's make sure you have everything you need for running this demo.

## Prerequisites

### Install .NET SDK
You'll need the latest .NET SDK for this workshop. Testcontainers libraries are compatible with .NET, and this workshop uses an ASP.NET Core application.

We recommend downloading the latest .NET SDK from the [official .NET website](https://dotnet.microsoft.com/download).

### Install Docker
You need to have a [Docker](https://docs.docker.com/get-docker/) or [Podman](https://podman.io/) environment to use Testcontainers.

```bash
$ docker version

Client:
 Version:           28.5.1
 API version:       1.51
 Go version:        go1.24.8
 Git commit:        e180ab8
 Built:             Wed Oct  8 12:16:17 2025
 OS/Arch:           darwin/arm64
 Context:           desktop-linux

Server: Docker Desktop 4.49.0 (208700)
 Engine:
  Version:          28.5.1
  API version:      1.51 (minimum version 1.24)
  Go version:       go1.24.8
  Git commit:       f8215cc
  Built:            Wed Oct  8 12:18:25 2025
  OS/Arch:          linux/arm64
  Experimental:     false
 containerd:
  Version:          1.7.27
  GitCommit:        05044ec0a9a75232cad458027ca83437aae3f4da
 runc:
  Version:          1.2.5
  GitCommit:        v1.2.5-0-g59923ef
 docker-init:
  Version:          0.19.0
  GitCommit:        de40ad0
```

## Download the project

Clone the [microcks-aspire-dotnet-demo](https://github.com/microcks/microcks-aspire-dotnet-demo) repository from GitHub to your computer:

```bash
git clone https://github.com/microcks/microcks-aspire-dotnet-demo.git
```

## Compile the project to download the dependencies

With .NET CLI:

```bash
dotnet restore
dotnet build
```

[Next](step2-exploring-the-app.md)