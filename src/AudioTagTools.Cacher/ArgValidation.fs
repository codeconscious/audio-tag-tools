module Cacher.ArgValidation

open Errors
open System.IO

let validate args : Result<(DirectoryInfo * FileInfo), CacherError> =
    match args with
    | [| mediaDirArg; tagLibArg |] ->
        if Directory.Exists mediaDirArg
        then Ok (DirectoryInfo mediaDirArg, FileInfo tagLibArg)
        else Error (MediaDirectoryMissing mediaDirArg)
    | _ ->
        Error ArgInvalidCount
