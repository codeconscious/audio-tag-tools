module Cacher.Library

open ArgValidation
open Errors
open IO
open Tags
open CCFSharpUtils.Library
open FsToolkit.ErrorHandling

let private run (args: string array) : Result<unit, Error> =
    result {
        let! mediaDir, tagLibraryFile = validate args
        let! fileInfos = getFileInfos mediaDir
        let! tagLibraryMap = createTagLibraryMap tagLibraryFile
        let! newJson = fileInfos |> generateJson tagLibraryMap

        let _ =
            tagLibraryFile
            |> File.copyToBackupFile "yyyyMMdd_HHmmss"
            |. fun backupFile -> printfn "Backed up previous file to \"%s\"." backupFile.Name
            |! WriteFileError

        do!
            newJson
            |> File.writeTextToFile tagLibraryFile.FullName
            |. fun _ -> printfn "Wrote file \"%s\"." tagLibraryFile.FullName
            |! WriteFileError
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok () -> Ok "Finished caching successfully!"
    | Error e -> Error (message e)
