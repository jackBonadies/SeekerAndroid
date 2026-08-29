# Soulseek.NET

[![NuGet version](https://img.shields.io/nuget/v/Soulseek.svg)](https://www.nuget.org/packages/Soulseek/)
[![Semantic Versioning](https://img.shields.io/badge/semver-2.0.0-3D9FE0.svg)](https://semver.org/)
[![license](https://img.shields.io/github/license/jpdillingham/Soulseek.NET.svg)](https://github.com/jpdillingham/Soulseek.NET/blob/master/LICENSE)

[![build](https://github.com/jpdillingham/Soulseek.NET/actions/workflows/build.yml/badge.svg)](https://github.com/jpdillingham/Soulseek.NET/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/jpdillingham/Soulseek.NET/branch/master/graph/badge.svg)](https://codecov.io/gh/jpdillingham/Soulseek.NET)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=security_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)

A .NET Standard client library for the Soulseek network.

# Documentation

The external interface of the library is sparse and well documented; the best resource is the code itself.  Of particular interest:

* [ISoulseekClient](https://github.com/jpdillingham/Soulseek.NET/blob/master/src/ISoulseekClient.cs)
* [SoulseekClientOptions](https://github.com/jpdillingham/Soulseek.NET/blob/master/src/Options/SoulseekClientOptions.cs)
* [SearchOptions](https://github.com/jpdillingham/Soulseek.NET/blob/master/src/Options/SearchOptions.cs)
* [TransferOptions](https://github.com/jpdillingham/Soulseek.NET/blob/master/src/Options/TransferOptions.cs)

## Excluded Search Phrases

Starting around the beginning of 2024, the Soulseek server has begun sending a list of 'excluded search phrases' as a way to restrict content exchanged on the network and appease copyright trolls.

This list of phrases is delivered in the event `ExcludedSearchPhrassReceived`, and it is my expectation that any outgoing search results _must_ be filtered to exclude files that contain any of the excluded phrases in the path or filename.

I appreciate everyone's cooperation and commitment to ensuring the long term health of the Soulseek network.

# Reserved Minor Version Ranges

Applications using this library are required, as a condition of the [LICENSE](https://github.com/jpdillingham/Soulseek.NET/blob/master/LICENSE), to use a unique minor version number when logging in to the Soulseek network.  To aid in avoidance of versions used by other applications, a list of reserved version ranges and their associated applications are included below.

* 760-7699999: [slskd](https://github.com/slskd/slskd)

# See also

## References

* [Nicotine+ (most up to date)](https://nicotine-plus.org/doc/SLSKPROTOCOL.html)
* [SoulseekProtocol - Museek+](https://www.museek-plus.org/wiki/SoulseekProtocol)
* [Soulseek Protocol Documentation (mirrored)](https://htmlpreview.github.io/?https://github.com/jpdillingham/Soulseek.NET/blob/master/docs/Soulseek%20Protocol%20Documentation.html)

## Alternative Clients and Libraries

Much of Soulseek.NET was made possible by the work of others. The following resources were used as a reference:

* [nicotine-plus](https://github.com/Nicotine-Plus/nicotine-plus)
* [livelook](https://github.com/misterhat/livelook) by @misterhat
* [museek-plus](https://github.com/eLvErDe/museek-plus) by @eLvErDe
* [slsk-client](https://github.com/f-hj/slsk-client) by @f-hj.