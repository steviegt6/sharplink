# sharplink

A fully managed [HashLink](https://github.com/HaxeFoundation/hashlink) virtual machine reimplementation for the CLI (.NET) platform.

## What is sharplink?

sharplink is a reimplementation of HashLink, functioning as a transpiler for HashLink bytecode binaries, allowing them to be turned into runnable CLI assemblies.

## Why is sharplink?

I felt like it.

## Status

> **TL;DR:** sharplink is not yet ready to be used; it cannot produce a working "Hello, world!" app, let alone transpile a full game.

Progress is being made fast, but sharplink is not yet ready to be used.

Long-term goals:

- [ ] transpile a "Hello, world!" app,
- [ ] rewrite compiler to produce more .NET-like IL (define methods instead of delegate fields, etc.).

Opcode implementation status (66/99, ~66%):

| Opcode Name     | ⚪[^1] |
| --------------- | ------ |
| Mov             | 🟢     |
| Int             | 🟢     |
| Float           | 🟢     |
| Bool            | 🟢     |
| Bytes           | 🔴     |
| String          | 🟢     |
| Null            | 🟢     |
| Add             | 🟢     |
| Sub             | 🟢     |
| Mul             | 🟢     |
| SDiv            | 🟢     |
| UDiv            | 🟢     |
| SMod            | 🟢     |
| UMod            | 🟢     |
| Shl             | 🟢     |
| SShr            | 🟢     |
| UShr            | 🟢     |
| And             | 🟢     |
| Or              | 🟢     |
| Xor             | 🟢     |
| Neg             | 🟢     |
| Not             | 🟢     |
| Incr            | 🟢     |
| Decr            | 🟢     |
| Call0           | 🟢     |
| Call1           | 🟢     |
| Call2           | 🟢     |
| Call3           | 🟢     |
| Call4           | 🟢     |
| CallN           | 🟢     |
| CallMethod      | 🟡     |
| CallThis        | 🟡     |
| CallClosure     | 🟡     |
| StaticClosure   | 🔴     |
| InstanceClosure | 🔴     |
| VirtualClosure  | 🔴     |
| GetGlobal       | 🟢     |
| SetGlobal       | 🟢     |
| Field           | 🟡     |
| SetField        | 🟡     |
| GetThis         | 🟡     |
| SetThis         | 🟡     |
| DynGet          | 🟡     |
| DynSet          | 🟡     |
| JTrue           | 🟢     |
| JFalse          | 🟢     |
| JNull           | 🟢     |
| JNotNull        | 🟢     |
| JSLt            | 🟢     |
| JSGte           | 🟢     |
| JSGt            | 🟢     |
| JSLte           | 🟢     |
| JULt            | 🟢     |
| JUGte           | 🟢     |
| JNotLt          | 🟢     |
| JNotGte         | 🟢     |
| JEq             | 🟢     |
| JNotEq          | 🟢     |
| JAlways         | 🟢     |
| ToDyn           | 🔴     |
| ToSFloat        | 🟢     |
| ToUFloat        | 🟢     |
| ToInt           | 🟢     |
| SafeCast        | 🔴     |
| UnsafeCast      | 🔴     |
| ToVirtual       | 🟡     |
| Label           | 🟢     |
| Ret             | 🟢     |
| Throw           | 🟢     |
| Rethrow         | 🔴     |
| Switch          | 🔴     |
| NullCheck       | 🟢     |
| Trap            | 🔴     |
| EndTrap         | 🔴     |
| GetI8           | 🔴     |
| GetI16          | 🔴     |
| GetMem          | 🔴     |
| GetArray        | 🔴     |
| SetI8           | 🔴     |
| SetI16          | 🔴     |
| SetMem          | 🔴     |
| SetArray        | 🔴     |
| New             | 🟢     |
| ArraySize       | 🔴     |
| Type            | 🔴     |
| GetType         | 🔴     |
| GetTID          | 🔴     |
| Ref             | 🟢     |
| Unref           | 🟢     |
| Setref          | 🔴     |
| MakeEnum        | 🔴     |
| EnumAlloc       | 🔴     |
| EnumIndex       | 🔴     |
| EnumField       | 🔴     |
| SetEnumField    | 🔴     |
| Assert          | 🔴     |
| RefData         | 🔴     |
| RefOffset       | 🔴     |
| Nop             | 🟢     |

[^1]: `🟢` = fully implemented, `🟡` = partially implemented/fully implemented but needs testing, `🔴` = not yet implemented.
