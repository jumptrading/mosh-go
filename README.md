Windows native port of original [Mobile Shell](https://github.com/mobile-shell/mosh) project.  
MinGW 32-bit and 64-bit toolchains are supported to build [mosh-client](client). No dependencies on MSYS or Cygwin.  
`mosh-server` Windows native build is not supported.

Instead of [original Perl based wrapper](https://github.com/mobile-shell/mosh/blob/335e3869b7af59314255a121ec7ed0f6309b06e7/scripts/mosh.pl) around `mosh-client` this port maintains [C#](mosh_cs) and [GoLang](mosh_go) wrappers.

`GoLang` mosh wrapper is taken from https://github.com/artyom/mosh and is not actively used for the moment.

Both `mosh-client` and `C# wrapper` are distributed in binary form as part of [Fluent Terminal](https://github.com/felixse/fluentterminal).

### Build mosh-client

You should have `MinGW` compiler and `CMake` pre-installed and available in your `%PATH%`.

```cmd
cd client && mkdir build && cd build
cmake -G "MinGW Makefiles" ..
cmake --build .
```

Prebuilt static libs of Protobuf, Zlib and OpenSSL reside in repo both for [x86](client/libs32) and [x64](client/libs) platforms.


### Build Protobuf with MinGW

```cmd
mkdir mingw_build && cd mingw_build
cmake -G "MinGW Makefiles" -Dprotobuf_BUILD_SHARED_LIBS=OFF -Dprotobuf_UNICODE=OFF -Dprotobuf_BUILD_TESTS=0 -DCMAKE_INSTALL_PREFIX=./ ../cmake
mingw32-make -j10 all
mingw32-make install
```

### Generate mosh protocol [CPP sources](client/mosh-client/protobufs) with Protobuf

```cmd
%PROTOBUF_PATH%/protoc.exe --cpp_out=. userinput.proto hostinput.proto transportinstruction.proto
```

### Build ZLib with MinGW

```cmd
mingw32-make.exe -fwin32/Makefile.gcc
```

### Prebuilt OpenSSL binaries

```
Download from https://bintray.com/vszakats/generic/download_file?file_path=openssl-1.1.1b-win64-mingw.zip
```


**LICENSE**: MIT
