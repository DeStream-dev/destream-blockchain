## DeStream DNS Crawler 
The DeStream DNS Crawler provides a list of DeStream full nodes that have recently been active via a custom DNS server.

### Prerequisites

To install and run the DNS Server, you need
* [.NET Core 2.0](https://www.microsoft.com/net/download/core)
* [Git](https://git-scm.com/)

## Build instructions

### Get the repository and its dependencies

```
git clone https://github.com/stratisproject/DeStreamBitcoinFullNode.git  
cd DeStreamBitcoinFullNode
git submodule update --init --recursive
```

### Build and run the code
With this node, you can run the DNS Server in isolation or as a DeStream node with DNS functionality:

1. To run a <b>DeStream</b> node <b>only</b> on <b>MainNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -dnslistenport=5399 -dnshostname=dns.stratisplatform.com -dnsnameserver=ns1.dns.stratisplatform.com -dnsmailbox=admin@stratisplatform.com
```  

2. To run a <b>DeStream</b> node and <b>full node</b> on <b>MainNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -dnsfullnode -dnslistenport=5399 -dnshostname=dns.stratisplatform.com -dnsnameserver=ns1.dns.stratisplatform.com -dnsmailbox=admin@stratisplatform.com
```  

3. To run a <b>DeStream</b> node <b>only</b> on <b>TestNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -testnet -dnslistenport=5399 -dnshostname=dns.stratisplatform.com -dnsnameserver=ns1.dns.stratisplatform.com -dnsmailbox=admin@stratisplatform.com
```  

4. To run a <b>DeStream</b> node and <b>full node</b> on <b>TestNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -testnet -dnsfullnode -dnslistenport=5399 -dnshostname=dns.stratisplatform.com -dnsnameserver=ns1.dns.stratisplatform.com -dnsmailbox=admin@stratisplatform.com
```  

### Command-line arguments

| Argument      | Description                                                                          |
| ------------- | ------------------------------------------------------------------------------------ |
| dnslistenport | The port the DeStream DNS Server will listen on                                       |
| dnshostname   | The host name for DeStream DNS Server                                                 |
| dnsnameserver | The nameserver host name used as the authoritative domain for the DeStream DNS Server |
| dnsmailbox    | The e-mail address used as the administrative point of contact for the domain        |

### NS Record

Given the following settings for the DeStream DNS Server:

| Argument      | Value                             |
| ------------- | --------------------------------- |
| dnslistenport | 53                                |
| dnshostname   | stratisdns.stratisplatform.com    |
| dnsnameserver | ns.stratisdns.stratisplatform.com |

You should have NS and A record in your ISP DNS records for your DNS host domain:

| Type     | Hostname                          | Data                              |
| -------- | --------------------------------- | --------------------------------- |
| NS       | stratisdns.stratisplatform.com    | ns.stratisdns.stratisplatform.com |
| A        | ns.stratisdns.stratisplatform.com | 192.168.1.2                       |

To verify the DeStream DNS Server is running with these settings run:

```
dig +qr -p 53 stratisdns.stratisplatform.com
```  
or
```
nslookup stratisdns.stratisplatform.com
```
