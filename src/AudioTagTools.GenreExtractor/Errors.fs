module Errors

type Error =
    | InvalidArgCount
    | IoReadError of string
    | IoWriteError of string
    | TagParseError of string

let message = function
    | InvalidArgCount -> "Invalid arguments. Pass in (1) your tag library path and (2) the desired path for your exported genres file."
    | IoReadError msg -> $"I/O read failure: {msg}"
    | IoWriteError msg -> $"I/O write failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
