ARG SDKVersion=6.0

ARG BuildOutput="/src/output"
ARG PublishOutput="/app/publish"
ARG TargetFramework=net6.0
ARG PublishReadyToRun=false
ARG PublishReadyToRunShowWarnings=false
ARG TieredCompilation=true
ARG TieredCompilationQuickJit=true
ARG Runtime=linux-x64
ARG Configuration=Release
ARG RestoreParams="--runtime ${Runtime} \
	-p:TargetFramework=${TargetFramework}"
	#--mount=type=secret,id=nugetconfig \ #(before dotnet cmd)for private nuget packs config
		#--configfile /run/secrets/nugetconfig \	#(after dotnet cmd)
	#--disable-parallel
ARG BuildParams="--no-restore \
	--runtime ${Runtime} \
	--configuration ${Configuration} \
	--framework ${TargetFramework} \
	--no-self-contained \
	--output ${BuildOutput}"
ARG PublishParams="--no-restore \
	--no-build \
	-p:PublishReadyToRun=${PublishReadyToRun} \
	-p:PublishReadyToRunShowWarnings=${PublishReadyToRunShowWarnings} \
	-p:TieredCompilation=${TieredCompilation} \
	-p:TieredCompilationQuickJit=${TieredCompilationQuickJit} \
	--configuration ${Configuration} \
	--framework ${TargetFramework} \
	--output ${PublishOutput} \
	/p:UseAppHost=false"

FROM mcr.microsoft.com/dotnet/aspnet:${SDKVersion} AS base
#RUN apt-get update && apt-get install wget -y; mkdir -p ~/.vs-debugger; wget https://aka.ms/getvsdbgsh -O ~/.vs-debugger/GetVsDbg.sh; chmod a+x ~/.vs-debugger/GetVsDbg.sh
#RUN . ~/.vs-debugger/GetVsDbg.sh
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDKVersion} AS build
ARG ServiceRootDir
ARG RestoreParams
ARG BuildParams
ARG PublishParams
WORKDIR /src
COPY --link CsprojBase.csproj CsprojBase.csproj
COPY ./Library/Library ./Library/Library
COPY --link ./User/UserDomain ./User/UserDomain
COPY --link ./Search/SearchDomain ./Search/SearchDomain
COPY ./$ServiceRootDir ./$ServiceRootDir
WORKDIR /src/$ServiceRootDir
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
	for p in **/*.csproj; \
	do \
		dotnet restore $p $RestoreParams; \
		dotnet build $p $BuildParams; \
		dotnet publish $p $PublishParams; \
	done;

FROM build AS publish

FROM base AS final
ARG MySolutionName
ARG WebProjectName
ENV WebProjectName=$WebProjectName
ENV MySolutionName=$MySolutionName
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet /app/${MySolutionName}.${WebProjectName}.dll --environment=${ASPNETCORE_ENVIRONMENT}"]
