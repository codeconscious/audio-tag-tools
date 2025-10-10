module ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<FileInfo * FileInfo, Error> =
    match args with
    | [| tagLibraryPath; genreFilePath |] ->
        Ok (FileInfo tagLibraryPath, FileInfo genreFilePath)
    | _ ->
        Error InvalidArgCount
