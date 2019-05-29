cmake_minimum_required(VERSION 3.13)
project(mosh-client)

set(LIB_MOSH_SRC mosh-client/libinterface.cpp
                mosh-client/stmclient.cpp
                mosh-client/terminaloverlay.cpp
                mosh-client/crypto/crypto.cc
                mosh-client/crypto/base64.cc
                mosh-client/crypto/ocb.cc
                mosh-client/network/compressor.cc
                mosh-client/network/network.cc
                mosh-client/network/transportfragment.cc
                mosh-client/protobufs/hostinput.pb.cc
                mosh-client/protobufs/transportinstruction.pb.cc
                mosh-client/protobufs/userinput.pb.cc
                mosh-client/statesync/completeterminal.cc
                mosh-client/statesync/user.cc
                mosh-client/terminal/parser.cc
                mosh-client/terminal/parseraction.cc
                mosh-client/terminal/parserstate.cc
                mosh-client/terminal/terminal.cc
                mosh-client/terminal/terminaldispatcher.cc
                mosh-client/terminal/terminaldisplay.cc
                mosh-client/terminal/terminaldisplayinit.cc
                mosh-client/terminal/terminalframebuffer.cc
                mosh-client/terminal/terminalfunctions.cc
                mosh-client/terminal/terminaluserinput.cc
                mosh-client/util/locale_utils.cc
                mosh-client/util/pty_compat.cc
                mosh-client/util/swrite.cc
                mosh-client/util/select.cc
                mosh-client/util/timestamp.cc
                mosh-client/util/mbrtowc_utf8
                mosh-client/win32compat/tncon.cc
                mosh-client/win32compat/dup_socket.cc)

set(MOSH_SRC mosh-client/mosh-client.cpp)

include_directories(mosh-client/win32compat)
include_directories(mosh-client/statesync)
include_directories(mosh-client/terminal)
include_directories(mosh-client/util)
include_directories(mosh-client)
include_directories(mosh-client/network)
include_directories(mosh-client/crypto)
include_directories(mosh-client/protobufs)
include_directories(mosh-client/openssl-1.1.1b-win64-mingw/include)

add_library(moshclient STATIC ${LIB_MOSH_SRC})
add_executable(${PROJECT_NAME} ${MOSH_SRC})

target_link_libraries(${PROJECT_NAME} -L${PROJECT_SOURCE_DIR}/libs)
target_link_libraries(${PROJECT_NAME} moshclient)
target_link_libraries(${PROJECT_NAME} -static -lprotobuf -lprotobuf-lite -lprotoc -lcrypto -lssl -pthread -lpthread -lws2_32 -lz)
