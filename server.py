#coding=utf-8

import threading
import sys
import os
import socket
import json
import hashlib


def calcSha1(filepath):
	f = open(filepath,'rb')
	md5obj = hashlib.md5()
	md5obj.update(f.read())
	hash = md5obj.hexdigest()
	f.close()
	return str(hash).upper()

def recv_data(connect,connect2):
	try:
		data = json.loads(connect2.recv(1024))
	except Exception,e:
		connect2.send("{'result':'1','descriptin':'json convert failed'}")
		print Exception,e
		return 

	appid = data['id']
	md5 = data['md5']
	size = int(data['size'])
	ver = data['version']

	saveDir = ""
	saveFile = ""
	updateJson = "update.json"

	data = json.load(open('file_config.json'))
	for item in data['data']:
		if (item['id'] == appid):
			saveDir = item['dir']
			saveFile = item['name'] + "_"+ver+".apk"
	
	if (not saveDir or not saveFile):
		connect2.send("{'result':'1','descriptin':'no info match id'}")
		return 

	fileName =saveDir + "/" + saveFile 
	try:
		fd = open(fileName,"w")
		while True:
			data = connect.recv(1024)
			if (not data):
				break;
			fd.write(data)

		fd.close()
	except Exception,e:
		print Exception,e
		connect2.send("{'result':'1','descriptin':'write file failed'}")
		return 

	# file check size
	if (os.path.getsize(fileName) != size):
		connect2.send("{'result':'1','descriptin':'file size wrong'}")
		return 
	# file check md5
	if (calcSha1(fileName).upper() != md5.upper()):
		connect2.send("{'result':'1','descriptin':'md5 check wrong'}")
		return 

	# update.json
	try:
		fileName = saveDir + "/" + updateJson;
		data = json.load(open(fileName))
		data['version'] = ver
		data['md5'] = md5
		data['size'] = str(size)
		data['appUrl'] = data['appUrl'][:data['appUrl'].rfind("/")+1] + saveFile
		json.dump(data,open(fileName,'w'))
	except Exception,e:
		print Exception,e
		connect2.send("{'result':'1','descriptin':'read and write update.json failed'}")
		return 
	

	connect2.send("{'result':'0','descriptin':'success'}")

def mylisten(port,port2):
	sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)  
	sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  
	sock.bind(("0.0.0.0", port))
	sock.listen(1)

	sock2 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)  
	sock2.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  
	sock2.bind(("0.0.0.0", port2))
	sock2.listen(1)

	while True:
		connection,address = sock.accept()
		connection.settimeout(50)

		connection2,address2 = sock2.accept()
		connection2.settimeout(50)

		try:
			recv_data(connection,connection2)
		except Exception,e:
			print  Exception,":",e

		connection.close()
		connection2.close()



mylisten(51007,51008)
