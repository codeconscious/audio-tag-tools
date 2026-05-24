module Shared.IO

open CCFSharpUtils.IO
open System.IO

let backUpFile : FileInfo -> Result<FileInfo,string> =
    File.backUpWithTimestamp timeStampFormat
