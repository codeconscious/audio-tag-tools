module Cacher.Library

open ArgValidation
open Errors
open IO
open Tags
open Shared.Constants
open CCFSharpUtils
open FsToolkit.ErrorHandling

let private run (args: string array) : Result<unit, CacherError> =
    result {
        let! mediaDir, tagLibraryFile = validate args
        let! fileInfos = getFileInfos mediaDir
        let! tagLibraryMap = createTagLibraryMap tagLibraryFile
        let! newJson = fileInfos |> generateJson tagLibraryMap

        let _ =
            tagLibraryFile
            |> backUpFile
            |. printfn "Backed up previous file to \"%O\"."
            |! FileWriteError

        do!
            newJson
            |> File.writeText' tagLibraryFile
            |. fun _ -> printfn "Wrote new file \"%s\"." tagLibraryFile.FullName
            |! FileWriteError
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished caching successfully!"
    | Error e -> Error (message e)
