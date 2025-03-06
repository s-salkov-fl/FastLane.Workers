Solution has 2 projects, needed to start in startup configuration
1) TaskServer - WebApi with swagger methods, which can be run and test from browser
- To Run some worker on host server's web method /WorkersManagement/Run can be used with such arguments
hostId - hostId on which run worker
workerTypeName - Type of worker's class in Assembly qualified format, for example - 'FastLane.Workers.Implementations.WorkerLong, FastLane.Workers, Version=1.0.0.0'. Three elements should be present, separated by comma: full name of type with namespaces, name of assembly, and version. This is mandatory for Assembly download from Server and runtime load workers assemblies, because relative location starting from WorkersAssemblyFolder, contains specific folder name for worker's dll in such format: FastLane.Workers_v1_0_0_0. It consists of Name of assembly, followed with underscore, and version with dots replaced by underscores too.
!If in the servers root folders with assemblies present such files FastLane.Workers_v1_0_0_0.zip, server will send them to hosts in case if they are missing it. Otherwise, server will create such archive from folder FastLane.Workers_v1_0_0_0 for example.
jsonInput - parameters for worker in Json format
2) HostService - Host executing Workers (can launch several copies on one computer)

Host's configuration
	"BusConfig": {
		"Host": "127.0.0.1",
		"Port": 5672,
		"UserName": "guest",
		"Password": "guest",
		"ResponseWaitTimeMilliseconds": 20000, - timeout in milliseconds, when the system considers the message to be outdated and does not process it. May change in the future
		"ServerId": "mainserver", - name of main server to which host will send messages
		"HostsBroadCastExchange": "fastlane.workers.hosts.broadcast", - exchange point for server to send messages to all online hosts
		"HostsDirectExchange": "fastlane.workers.hosts.direct", - exchange point for server to send direct messages host, using key as Id in HostConfig section below
		"ServersExchangeTemplate": "fastlane.workers.servers.{0}", - exchange point to which hosts send messages to server
		"QueueServerTemplate": "fastlane.workers.servers.{0}", - queue to which hosts send messages to server(used by server's listener)
		"QueueHostTemplate": "fastlane.workers.hosts.{0}" - template of queue name for specific host based on Id of host in HostConfig(vvv)
	},
	"HostConfig": {
		"Id": "host1", - Host ID, used to configure exchange points and queues on the bus
		"WorkersAssemblyFolder": "%Temp%\\WorkerAssemblies" - Folder where worker assemblies are stored
		"StartFailIfQueueAlreadyInUse": false - do not start host if its queue using another consumer
	}

Server's configuration
 ...
  "ServerConfig": {
        "PingHostsStatusPeriodMilliseconds": 10000, - period for polling host availability statuses by the server 
        "PingRequestResponseTimeOutMs": 5000, - timeout for ping server's request when it counts some host as unavailable
        "WorkersAssemblyFolder": "%Temp%\\WorkerAssemblies" - Folder where zipped worker assemblies are stored. From this location server sends worker packages to hosts by request
    }


