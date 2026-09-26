# Publication Workflow

The publication workflow is a staged subscription lifecycle:

```text
Open Subscription
-> Read Publication
-> Remove Publication
-> Close Subscription
```

## Identifiers

`Open Subscription` returns a `sessionId`. Pass that session ID to read, remove, and close operations.

`Read Publication` may return a publication `messageId` and payload. Publication removal acts on the current item in the subscription session and does not require a message ID argument in the Toolkit commands.

## Python Flow

Run the numbered publication samples in order:

```text
01_open_subscription.py
02_read_publication.py
03_remove_publication.py
04_close_subscription.py
```

The samples save demo state in `samples/python/sample_state.local.json`. That file is ignored by Git and is only local tutorial state.

## CLI Flow

The same staged flow is available directly through the CLI:

```powershell
MCEGold.Data.Services.Connector.Cli.exe publication open-subscription --config configs/connector.config.development.json
MCEGold.Data.Services.Connector.Cli.exe publication read --config configs/connector.config.development.json --session-id <session-id>
MCEGold.Data.Services.Connector.Cli.exe publication remove --config configs/connector.config.development.json --session-id <session-id>
MCEGold.Data.Services.Connector.Cli.exe publication close-subscription --config configs/connector.config.development.json --session-id <session-id>
```

## Atomic `publication receive`

`publication receive` is a convenience workflow that opens a subscription, reads one publication, optionally removes it, and closes the subscription.

Use staged commands or the numbered Python samples when your application needs to own session state explicitly.

## Cleanup

Always close remote sessions when possible. Deleting local state files does not close remote sessions.
