module Hive.Test.Utilities

let throwsAny (f:unit->unit) = 
    try
        f()
        false
    with
    | _ -> true

type TestResult<'Expected,'Actual> = 
    {
        IsCorrect : bool
        TestId: int
        Expected : 'Expected
        Actual : 'Actual
    }
    override this.ToString() = sprintf "%i %b" this.TestId this.IsCorrect

type ILogger<'Message> =
    abstract member Post : 'Message -> unit
    abstract member Messages : 'Message list
    abstract member Clear : unit

type private LoggerMessage<'Message> =
    | Query of AsyncReplyChannel<'Message list>
    | Post of 'Message
    | Clear

let logger<'Message> =
    let mailbox =
       MailboxProcessor<LoggerMessage<'Message>>.Start(fun inbox ->
        async { 
            let messages = ref List.empty
            while true do
                let! message = inbox.Receive()
                match message with
                | Post m -> messages := m :: !messages
                | Query channel -> channel.Reply (!messages |> List.rev)
                | Clear -> messages := list.Empty
        })
    { new ILogger<_> with
        member this.Post message = mailbox.Post (LoggerMessage<_>.Post message) 
        member this.Messages = mailbox.PostAndReply (fun replyChannel -> LoggerMessage<_>.Query replyChannel)
        member this.Clear = mailbox.Post LoggerMessage<_>.Clear
    }