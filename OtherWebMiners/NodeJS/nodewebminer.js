//requires
var http = require('http');
var net = require('net');
var tls = require('tls');

var httpServer = http.createServer(listenHandler);

function listenHandler(request, response) {
    if (request.method === 'GET') {
        response.writeHead(200, { 'SCV-Miner': 'Nodejs', 'Content-Type': 'text/html' });
        response.end('Nodejs Miner is working!');
    } else if (request.method === 'POST') {
        try {
            var scvPackage = {
                host: request.headers['scv-host'],
                port: request.headers['scv-port'],
                ip: request.headers['scv-ip'],
                isSsl: request.headers['scv-ssl'] && request.headers['scv-ssl'].toLowerCase() === 'true',
                isEncrypted: request.headers['scv-encrypted'] && request.headers['scv-encrypted'].toLowerCase() === 'true',
                data: new Buffer(0)
            };
            request.on('data', function (data) {
                try {
                    scvPackage.data = Buffer.concat([scvPackage.data, data]);
                    if (scvPackage.data.length >= request.headers['content-length']) {
                        fetch(scvPackage, response);
                    }
                } catch (err) {
                    response.writeHead(200, { 'Content-Type': 'text/html', 'SCV-Exception': 'Exception' });
                    response.end(err);
                }
            });
        } catch (err) {
            response.writeHead(200, { 'Content-Type': 'text/html', 'SCV-Exception': 'Exception' });
            response.end(err);
        }
    }
};

function fetch(scvPackage, response) {
    var socket = new net.Socket();
    var contentData = new Buffer(0);
    var encryptionProvider;
    if (scvPackage.isEncrypted) {
        encryptionProvider = new EncryptionProvider(scvPackage.host);
        encryptionProvider.Decrypt(scvPackage.data);
    }
    function ondata(data) {
        try {
            contentData = Buffer.concat([contentData, data]);
            if (new HttpPackage(contentData).isValid) {
                if (scvPackage.isEncrypted) {
                    encryptionProvider.Encrypt(contentData);
                }
                response.writeHead(200, { 'Content-Type': 'image/gif', 'Content-Length': contentData.length });
                response.end(contentData);
            }
        } catch (err) {
            response.writeHead(200, { 'SCV-Miner': 'Nodejs', 'Content-Type': 'text/html', 'SCV-Exception': 'Exception' });
            response.end(err);
        }
    };

    function onend() {
        try {
            response.writeHead(200, { 'Content-Type': 'image/gif', 'Content-Length': contentData.length });
            if (scvPackage.isEncrypted) {
                encryptionProvider.Encrypt(contentData);
            }
            response.end(contentData);
        } catch (err) {
            response.writeHead(200, { 'SCV-Miner': 'Nodejs', 'Content-Type': 'text/html', 'SCV-Exception': 'Exception' });
            response.end(err);
        }
    };

    function onerror(e) {
        response.writeHead(200, { 'Content-Type': 'image/gif', 'SCV-Exceptoin': 'Exception' });
        response.end(e);
    };

    socket.connect(scvPackage.port, (scvPackage.ip || scvPackage.host), function () {
        try {
            if (!scvPackage.isSsl) {
                socket.on('data', ondata);
                socket.on('end', onend);
                socket.on('error', onerror);
                socket.write(scvPackage.data);
            } else {
                var cleartextStream = tls.connect({ socket: socket, servername: scvPackage.host }, function () {
                    cleartextStream.on('data', ondata);
                    cleartextStream.on('end', onend);
                    cleartextStream.on('error', onerror);
                    cleartextStream.write(scvPackage.data);
                });
            }
        } catch (err) {
            response.writeHead(200, { 'SCV-Miner': 'Nodejs', 'Content-Type': 'text/html', 'SCV-Exception': 'Exception' });
            response.end(err);
        }
    });
};

function HttpPackage(data) {
    this.isValid = false;

    var dataStr = data.toString('ascii');
    var splitIndex = dataStr.indexOf('\r\n\r\n');
    if (splitIndex) {
        var contentOffset = splitIndex + 4;
        var headerStr = dataStr.substring(0, splitIndex);
        var headerArray = headerStr.split('\r\n');
        var contentLength = -1;
        var connectionClose = false;
        var contentChunked = false;
        for (var i = 1; i < headerArray.length; i++) {
            var ha = headerArray[i].split(/\:\s?/);
            if (ha[0] === 'Content-Length') {
                contentLength = +ha[1];
                break;
            } else if (ha[0] === 'Connection' && ha[1].toLowerCase() === 'close') {
                connectionClose = false;
                break;
            } else if (ha[0] === 'Transfer-Encoding' && ha[1].toLowerCase() === 'chunked') {
                contentChunked = true;
                break;
            }
        }
        if (contentLength === -1 && !connectionClose && !contentChunked) {
            contentLength = 0;
        }

        if (contentLength >= 0) {
            this.isValid = (contentOffset + contentLength === data.length);
        } else if (connectionClose) {
            this.isValid = true;
        } else if (contentChunked) {
            for (var j = contentOffset, cl = 0; j < data.length; cl = 0) {
                for (var temp = data[j]; temp != 0x0D && j < data.length; temp = data[++j]) {
                    cl = cl * 16 + (temp > 0x40 ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9 : temp - 0x30);
                }
                if (cl === 0) {
                    this.isValid = true;
                    return;
                } else {
                    j += (cl + 4);
                }
            }
        }
    }
};

function EncryptionProvider(key) {
    this.seed = new Array(16);
    var ka = key ? key : "!1@2#3$4%5^6&7*8";
    var kl = ka.length - 1;
    for (var i = 0; i < 16; i++) {
        this.seed[i] = ((ka.charCodeAt(i & kl) + kl) & 85);
    }

    this.Encrypt = function (src, length) {
        var len = (length === undefined || length > src.length ? src.length : length);
        for (var i = 0; i < len; i++) {
            var steps = (i & 7) + ((i & 8) == 0 ? -8 : 1);
            src[i] = ~((src[i] + steps * this.seed[i & 15]) & 255);
        }
    };

    this.Decrypt = function (src, length) {
        var len = length === undefined || length > src.length ? src.length : length;
        for (var i = 0; i < len; i++) {
            var steps = (i & 7) + ((i & 8) == 0 ? -8 : 1);
            src[i] = ((~src[i] - steps * this.seed[i & 15]) & 255);
        }
    };
};

httpServer.listen(9000);

console.log('Server running at http://127.0.0.1:9000/');
