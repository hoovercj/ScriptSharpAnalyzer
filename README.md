# ScriptSharp Roslyn Analyzer
This is a simple project that analyzes ScriptSharp code to warn me when I use unsupported features.

There are currently a few syntax kinds that cause errors:
- Lambdas
- Object and collection initializers
- Interfaces extending other Interfaces
- Auto-properties
- Query expressions

If I am feeling ambitious I will add additional checks and/or code fixes, or if you're interested in contributing open an issue or a pull request.