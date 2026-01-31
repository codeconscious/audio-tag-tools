module Cacher.ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<DirectoryInfo * FileInfo, Error> =
    match args with
    | [| mediaDirPath; tagLibraryPath |] ->
        match Directory.Exists mediaDirPath with
        | false -> Error (MediaDirectoryMissing mediaDirPath)
        | true  -> Ok (DirectoryInfo mediaDirPath, FileInfo tagLibraryPath)
    | _ ->
        Error InvalidArgCount
