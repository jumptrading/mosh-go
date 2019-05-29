.PHONY: generate-mosh-protobufs
generate-mosh-protobufs:
	C:/work/protobuf/prot1/protobuf/build_mingw/bin/protoc --cpp_out=. userinput.proto hostinput.proto transportinstruction.proto


.PHONY: mosh-client
mosh-client:
	g++ -o mosh-client.exe mosh-client/mosh-client.cpp \
            mosh-client/stmclient.cpp \
            mosh-client/terminaloverlay.cpp \
            mosh-client/crypto/crypto.cc \
            mosh-client/crypto/base64.cc \
            mosh-client/crypto/ocb.cc \
            mosh-client/network/compressor.cc \
            mosh-client/network/network.cc \
            mosh-client/network/transportfragment.cc \
            mosh-client/protobufs/hostinput.pb.cc \
            mosh-client/protobufs/transportinstruction.pb.cc \
            mosh-client/protobufs/userinput.pb.cc \
            mosh-client/statesync/completeterminal.cc \
            mosh-client/statesync/user.cc \
            mosh-client/terminal/parser.cc \
            mosh-client/terminal/parseraction.cc \
            mosh-client/terminal/parserstate.cc \
            mosh-client/terminal/terminal.cc \
            mosh-client/terminal/terminaldispatcher.cc \
            mosh-client/terminal/terminaldisplay.cc \
            mosh-client/terminal/terminaldisplayinit.cc \
            mosh-client/terminal/terminalframebuffer.cc \
            mosh-client/terminal/terminalfunctions.cc \
            mosh-client/terminal/terminaluserinput.cc \
            mosh-client/util/locale_utils.cc \
            mosh-client/util/pty_compat.cc \
            mosh-client/util/swrite.cc \
            mosh-client/util/select.cc \
            mosh-client/util/timestamp.cc \
            mosh-client/win32compat/tncon.cc \
            mosh-client/win32compat/dup_socket.cc \
        -I mosh-client/win32compat \
        -I mosh-client/statesync \
        -I mosh-client/terminal \
        -I mosh-client/util \
        -I mosh-client \
        -I mosh-client/network \
        -I mosh-client/crypto \
        -I mosh-client/protobufs \
        -I mosh-client/openssl-1.1.1b-win64-mingw/include \
            -L libs -static -lprotobuf -lprotobuf-lite -lprotoc -lcrypto -lssl -pthread -lpthread -lws2_32 -lz
