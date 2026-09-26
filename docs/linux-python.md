# Linux / Python

The Linux package bundles a self-contained `linux-x64` CLI with the Python wrapper samples.

Artifact:

```text
mcegold-python-linux-x64-v1.0.0.tar.gz
```

The bundled CLI includes the .NET runtime required to run it. No system .NET runtime is required for package users.

Python 3.10 or later is required. The Python samples use only the Python standard library.

## First Run

1. Extract the archive:

   ```bash
   tar -xzf mcegold-python-linux-x64-v1.0.0.tar.gz
   cd mcegold-python-linux-x64-v1.0.0
   ```

2. Confirm the CLI is executable:

   ```bash
   test -x ./MCEGold.Data.Services.Connector.Cli
   ./MCEGold.Data.Services.Connector.Cli --help
   ```

3. Copy the config template:

   ```bash
   cp configs/connector.config.example.json configs/connector.config.development.json
   ```

4. Edit `configs/connector.config.development.json` and fill in connection settings.

5. Run the numbered Python samples.

## Publication Samples

Run these in order:

```bash
python3 samples/python/01_open_subscription.py
python3 samples/python/02_read_publication.py
python3 samples/python/03_remove_publication.py
python3 samples/python/04_close_subscription.py
```

## Request Samples

Run these in order:

```bash
python3 samples/python/05_open_request_session.py
python3 samples/python/06_post_request.py
python3 samples/python/07_read_response.py
python3 samples/python/08_remove_response.py
python3 samples/python/09_close_request_session.py
```

`07_read_response.py` is optional before remove when your workflow does not need to inspect the response first.

## Request JSON From Stdin

`06_post_request.py` can forward request JSON from stdin to the bundled CLI:

```bash
printf '{"requestType":"GetSites"}\n' | \
python3 samples/python/06_post_request.py --input -
```

Literal inline JSON as the direct `--input` argument is not supported. Use `--input <file>` or pipe JSON with `--input -`.

## More Detail

- [Configuration](configuration.md)
- [Request Workflow](request-workflow.md)
- [Publication Workflow](publication-workflow.md)
- [Supported Request Types](request-types.md)
- [CLI Reference](cli-reference.md)
- [Troubleshooting](troubleshooting.md)
