# rt2.1 toolchain (LLVM 23.1.1, for ARM32 Windows)

This is clang-cl and lld-link for building C and C++ for ARM32 Windows (`thumbv7`). It's LLVM
23.1.1 with 16 patches. It runs on 64-bit x86 Windows.

Home page: https://github.com/hamed7ir/llvm-rt1

## Install

Unpack it anywhere and put `bin` on your `PATH`, or point your build system at it. The Microsoft VC
runtime DLLs the tools need are already in `bin`. clang's own headers are in `lib/clang/23/include`.

It does **not** include Microsoft's C and C++ headers and libraries or the Windows SDK, and your
programs need those. For ARM32 you need Visual Studio 2022 Build Tools with the ARM build tools, and
a Windows SDK that still has ARM32 libraries (10.0.19041 has them, 10.0.26100 doesn't). For example:

```
clang-cl --target=thumbv7-pc-windows-msvc -fuse-ld=lld -vctoolsdir "<VS>\VC\Tools\MSVC\14.44.35207" -winsdkdir "<Windows Kits>\10" -winsdkversion 10.0.19041.0 hello.c
```

For Chromium (GN), these are the toolchain lines from my args:

```
clang_base_path = "<this folder>"
clang_version = "23"
clang_use_chrome_plugins = false    # rt2.1 doesn't have Chromium's clang plugins
```

## Check it

```
clang-cl --version
```

should print `Varan clang version 23.1.1-rt2.1 (e240ad76c3a4429084c026fe0a9b0fce34025032)`.

## What's in here

| | |
|---|---|
| `bin/` | clang, clang-cl, lld-link and the other tools, `libclang.dll`, and 8 Microsoft VC runtime DLLs |
| `lib/clang/23/include/` | clang's own headers |
| `lib/clang/23/lib/windows/` | compiler-rt: builtins for ARM, i386 and x86_64, and the x86_64 profile library |
| `staged/rtdiv/` | a fallback ARM builtins library. You should not need it, see its `README.txt` |
| `runtime/` | the opt-in `__aeabi` divide shim, see `README-aeabi-shim.txt` |
| `patches/` | the full diff this was built from, and every patch on its own |
| `licenses/` | libxml2's license (it's built into lld-link and llvm-mt) |
| `RELEASE-NOTES.md` | what changed, what works differently now, how it was tested |
| `MODIFICATIONS.txt` | the license notice for changed files |
| `LICENSE.TXT` | LLVM's license (Apache 2.0 with LLVM Exceptions) |

Read `RELEASE-NOTES.md` before you build something big with it, especially *Things that work
differently now*.
