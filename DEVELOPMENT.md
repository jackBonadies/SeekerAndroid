# Development

## Publishing

```bash
dotnet publish -c Release -f net9.0-android -p:AndroidSigningKeyPass=$keystorepass -p:AndroidSigningStorePass=$keystorepass
```
