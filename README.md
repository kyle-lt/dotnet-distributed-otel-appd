# dotnet-distributed-otel-appd (a ToDo app)
## Overview
This project was developed in order to get hands-on experience instrumenting a .NET Core application using the OpenTelemetry instrumentation libraries (notably Hosting, HTTP), as well as instrumenting with an Enterprise-class APM Platform (AppDynamics).

It keeps up-to-date pretty well with the [OTel Releases](https://github.com/open-telemetry/opentelemetry-dotnet/releases), and is currently using 1.0.0-rc3 (where possible, otherwise 1.0.0-rc2), using .NET Core 5.0.

   > __Note:__  This project was built/tested only on Docker for Mac

It uses the [OpenTelemetry Collector](https://github.com/open-telemetry/opentelemetry-collector), which is decoupled from this project and has to be setup separately.  The OOB configuration exports traces using the Jaeger Exporter, Logging Exporter, and AppDynamics (via OTLPHTTP Exporter). 

There is no guarantee that this application is built to any best practices or standards, and in certain cases is explicitly designed to __not__ be performant, and so from the angle of tracing and monitoring, it's all good.

It's not necessary to build this project.  All images can be pulled from Docker Hub when you run with [Docker Compose](#quick-start-with-docker-compose) or with [Kubernetes](#kubernetes).

   > __Note:__  Standing up the OpenTelemetry Collector is still required, even if you use the project's images

Once up and running, assuming you are running on your local machine, access the Home Page at `http://localhost:60000`.

![Home Page](/README_Images/Todo_TodoMvcUi_Home.png)

## Quick Start with Docker Compose
### Prerequisites
In order to run this project, you'll need:
- Docker
- Docker Compose
- OpenTelemetry Collector
  - Reference the [Otel Collector Demo](https://github.com/open-telemetry/opentelemetry-collector/tree/main/examples/demo)
  - Also, an example in this [Local Monitoring Stack](https://github.com/kyle-lt/local-monitoring-stack) Repo that uses a simple setup (no OpenTelemetry Collector Agent required)  
  <br />  

   > __Note:__  The Docker versions must support Docker Compose File version 3.2+

### Steps to Run
1. Clone this repository to your local machine.
2. Configure the `.env` file in the root project directory.

   > __IMPORTANT:__ Detailed information regarding `.env` file can be found [below](#env-file).  This __MUST__ be done for this project to work!
3. Use Docker Compose to start
```bash
$ docker-compose up -d
```
4. Access front-end UI on `http://$DOCKER_HOSTNAME:60000`.

   > __Note:__  Default configuration assumes localhost/127.0.0.1, so navigate to `http://127.0.0.1:60000`.

## Build
> __Note:__ the build process requires internet access.
### Prerequisites
If you'd like to build the project locally, you'll need:
- .NET Core SDK 3.1+
- Docker
- Docker Compose

### Steps to Build
1. Clone this repository to your local machine.
2. Build using docker-compose
```bash
$ docker-compose build
```
3. Configure the `.env` file in the root project directory.

   > __IMPORTANT:__ Detailed information regarding `.env` file can be found [below](###-.env-File).  This __MUST__ be done for this project to work!
4. Uncomment the build directives for the `todomvcui` and `todoapi` services in `docker-compose.yml`.
```bash
todoapi:
  ...
  build:
    context: .
    dockerfile: TodoApi/docker/Dockerfile
...
todomvcui:
  ...
  build:
    context: .
    dockerfile: TodoMvcUi/docker/Dockerfile
```
5. Use Docker Compose to start (or use [Kubernetes](#kubernetes))
```bash
$ docker-compose up -d
```

## Docker Compose Services
### todomvcui
Front-End ASP.NET Core MVC.  

By default, accessible on `http://$DOCKER_HOSTNAME:60000`.

### todoapi
ASP.NET Web API using in-memory database. 

By default, accessible on `http://$DOCKER_HOSTNAME:5000/api/TodoItems`.

### rabbitmq
RabbitMQ Alpine image, pulled from [`rabbitmq:3-management-alpine`](https://hub.docker.com/_/rabbitmq).

### jaeger
Jaeger all-in-one, pulled from [`jaegertracing/all-in-one:latest`](https://hub.docker.com/r/jaegertracing/all-in-one). 

Accessible on `http://$DOCKER_HOSTNAME:16686`

## More Notes on Configuration
### Project File Structure
Abbreviated tree output (only relevant files & paths shown)
```bash
$ tree -L 3
.
├── LICENSE.txt
├── README.md
├── README_Images
│   └── Todo_TodoMvcUi_Home.png
├── TodoApi
│   ├── Controllers
│   │   ├── TodoItemsController.cs
│   │   └── WeatherForecastController.cs
│   ├── Helpers
│   │   └── RabbitMqReceiver.cs
│   ├── Migrations
│   │   ├── 20200904122958_InitialCreate.Designer.cs
│   │   ├── 20200904122958_InitialCreate.cs
│   │   └── TodoContextModelSnapshot.cs
│   ├── Models
│   │   ├── TodoContext.cs
│   │   └── TodoItem.cs
│   ├── Program.cs
│   ├── Properties
│   │   └── launchSettings.json
│   ├── Startup.cs
│   ├── TodoApi.csproj
│   ├── TodoDb.db
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── bin
│   │   └── Debug
│   ├── docker
│   │   └── Dockerfile
├── TodoMvcUi
│   ├── Controllers
│   │   └── HomeController.cs
│   ├── Models
│   │   ├── ErrorViewModel.cs
│   │   ├── TodoItem.cs
│   │   ├── TodoItemDTO.cs
│   │   └── TodoItemList.cs
│   ├── Program.cs
│   ├── Properties
│   │   └── launchSettings.json
│   ├── Startup.cs
│   ├── TodoMvcUi.csproj
│   ├── Views
│   │   ├── Home
│   │   ├── Shared
│   │   ├── _ViewImports.cshtml
│   │   └── _ViewStart.cshtml
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── docker
│   │   └── Dockerfile
│   └── wwwroot
│       ├── css
│       ├── favicon.ico
│       ├── js
│       └── lib
├── Utils
│   ├── Messaging
│   │   ├── MessageReceiver.cs
│   │   ├── MessageSender.cs
│   │   └── RabbitMqHelper.cs
│   ├── Utils.csproj
├── docker-compose.yml
├── docker-compose.yml_private
├── downloadDotNetLinuxAgentLatest.sh
├── kubernetes
│   ├── jaeger-list.yaml
│   ├── rabbitmq.yml
│   ├── todoapi-deployment.yaml
│   ├── todoapi-deployment.yaml_private
│   ├── todomvcui-deployment.yaml
│   └── todomvcui-deployment.yaml_private
├── make-private.sh
└── make-public.sh
```
### Application Code
The app code is housed in `TodoMvcUi` and `TodoApi` directories.  Each directory contains source code and a `docker` directory.  The `docker` directory contains:
- `Dockerfile`

### AppD Agent
- The root project directory contains `downloadDotNetLinuxAgentLatest.sh`
   - This script is used during Docker image build to download the latest AppDynamics .NET Linux Agent and "bake" into the image.

### docker-compose.yml
This file is located in the project root and manages building and running the Docker containers. It uses the `.env` file to populate environment variables for the project to work properly.

### .env File
This file contains all of the environment variables that need to be populated in order for the project to run, and for the performance tools to operate.  Items that *must* be tailored to your environment are:

#### AppDynamics Controller Configuration
```bash
# APPD
APPDYNAMICS_AGENT_ACCOUNT_ACCESS_KEY=<Access_Key>
APPDYNAMICS_AGENT_ACCOUNT_NAME=<Account_Name>
APPDYNAMICS_CONTROLLER_HOST_NAME=<Controller_Host>
APPDYNAMICS_CONTROLLER_PORT=<Controller_Port>
APPDYNAMICS_CONTROLLER_SSL_ENABLED=<true_or_false>
```
> __Tip:__  Documentation on these configuration properties can be found in the [AppDynamics .NET Agent for Linux Configuration Documentation](https://docs.appdynamics.com/display/PRO45/.NET+Agent+for+Linux+Environment+Variables)

#### AppDynamics Browser EUM Configuration // TODO - Implement!
> __Note:__  You must create a Browser Application in AppDynamics (E.g., Todo-Web), and then copy the App Key into the configuration property below.  The remaining default configurations below can be left alone if using __SaaS__, but need to be provided for __on-prem__ deployments of the AppD EUM Collector.
```bash
# AppD Browser EUM
APPDYNAMICS_BROWSER_EUM_APPKEY=AA-AAA-AAA-AAA
APPDYNAMICS_BROWSER_EUM_ADRUM_EXT_URL_HTTP=http://cdn.appdynamics.com
APPDYNAMICS_BROWSER_EUM_ADRUM_EXT_URL_HTTPS=https://cdn.appdynamics.com
APPDYNAMICS_BROWSER_EUM_BEACON_HTTP=http://col.eum-appdynamics.com
APPDYNAMICS_BROWSER_EUM_BEACON_HTTPS=https://col.eum-appdynamics.com
```
> __Tip:__  Documentation on these configuration properties can be found in the [AppDynamics Real User Monitoring Documentation](https://docs.appdynamics.com/display/PRO45/Set+Up+and+Access+Browser+RUM)

**The rest of the environment variables in the `.env` file can be left with default values.**

## Kubernetes
This repo contains a few Kubernetes specs to deploy the app components and Jaeger as Kubernetes resources. They are located in the [kubernetes](/kubernetes) directory.
- `todomvuui-deployment.yaml`
  - This spec contains a single-replica Deployment and Service (using NodePort by default) for the Todo MVC UI.
- `todoapi-deployment.yaml`
  - This spec contains a single-replica Deployment and Service (using NodePort by default) for the Todo API.
- `jaeger-list.yaml`
  - This spec is a list containing a single-replica Deployment and a few Services (using NodePort and ClusterIP) for the Jaeger all-in-one components.

