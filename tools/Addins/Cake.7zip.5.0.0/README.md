# Cake.7zip

[![standard-readme compliant][]][standard-readme]
[![Contributor Covenant][contrib-covenantimg]][contrib-covenant]
[![Build][buildimage]][build]
[![Codecov Report][codecovimage]][codecov]
[![NuGet package][nugetimage]][nuget]
[![All Contributors][all-contributors-badge]](#contributors)

Makes [7zip](https://7-zip.org/) available as a tool in [cake](https://cakebuild.net/)

## Table of Contents

- [Install](#install)
- [Usage](#usage)
- [Discussion](#discussion)
- [Maintainer](#maintainer)
- [Contributing](#contributing)
  - [Contributors](#contributors)
- [License](#license)

## Install

```cs
#tool nuget:?package=7-Zip.CommandLine
#addin nuget:?package=Cake.7zip
```

## Usage

See also the [local documentation][documentation] and [api][api]

### Adding files

```cs
#tool nuget:?package=7-Zip.CommandLine
#addin nuget:?package=Cake.7zip

SevenZip(s => s
  .InAddMode()
  .WithArchive(File("fluent.zip"))
  .WithArchiveType(SwitchArchiveType.Zip)
  .WithFiles(File("a.txt"), File("b.txt"))
  .WithVolume(700, VolumeUnit.Megabytes)
  .WithCompressionMethodLevel(9));
```

### Extracting files

```cs
#tool nuget:?package=7-Zip.CommandLine
#addin nuget:?package=Cake.7zip

SevenZip(s => s
  .InExtractMode()
  .WithArchive(File("path/to/file.zip"))
  .WithArchiveType(SwitchArchiveType.Zip)
  .WithOutputDirectory("some/other/directory"));
```

## Discussion

If you have questions, search for an existing one, or create a new discussion on the Cake GitHub repository, using the `extension-q-a` category.

[![Join in the discussion on the Cake repository](https://img.shields.io/badge/GitHub-Discussions-green?logo=github)](https://github.com/cake-build/cake/discussions)

## Maintainer

[Nils Andresen @nils-a][maintainer]

## Contributing

Cake.7zip follows the [Contributor Covenant][contrib-covenant] Code of Conduct.

We accept Pull Requests.

Small note: If editing the Readme, please conform to the [standard-readme][] specification.

This project follows the [all-contributors][] specification. Contributions of any kind welcome!

### Contributors

Thanks goes to these wonderful people ([emoji key][emoji-key]):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="http://www.nils-andresen.de/"><img src="https://avatars3.githubusercontent.com/u/349188?v=4?s=100" width="100px;" alt="Nils Andresen"/><br /><sub><b>Nils Andresen</b></sub></a><br /><a href="https://github.com/cake-contrib/Cake.7zip/commits?author=nils-a" title="Code">💻</a> <a href="https://github.com/cake-contrib/Cake.7zip/commits?author=nils-a" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/DiDoHH"><img src="https://avatars.githubusercontent.com/u/45682415?v=4?s=100" width="100px;" alt="DiDoHH"/><br /><sub><b>DiDoHH</b></sub></a><br /><a href="https://github.com/cake-contrib/Cake.7zip/commits?author=DiDoHH" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/stehlih"><img src="https://avatars.githubusercontent.com/u/43820014?v=4?s=100" width="100px;" alt="Heiko Stehli"/><br /><sub><b>Heiko Stehli</b></sub></a><br /><a href="https://github.com/cake-contrib/Cake.7zip/commits?author=stehlih" title="Code">💻</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

## License

[MIT License © Nils Andresen][license]

[all-contributors]: https://github.com/all-contributors/all-contributors
[all-contributors-badge]: https://img.shields.io/github/all-contributors/cake-contrib/cake.7zip/develop?&style=flat-square
[build]: https://github.com/cake-contrib/Cake.7zip/actions/workflows/build.yml
[buildimage]: https://github.com/cake-contrib/Cake.7zip/actions/workflows/build.yml/badge.svg
[codecov]: https://codecov.io/gh/cake-contrib/Cake.7zip
[codecovimage]: https://img.shields.io/codecov/c/github/cake-contrib/Cake.7zip.svg?logo=codecov&style=flat-square
[contrib-covenant]: https://www.contributor-covenant.org/version/2/0/code_of_conduct/
[contrib-covenantimg]: https://img.shields.io/badge/Contributor%20Covenant-v2.0%20adopted-ff69b4.svg
[emoji-key]: https://allcontributors.org/docs/en/emoji-key
[maintainer]: https://github.com/nils-a
[nuget]: https://nuget.org/packages/Cake.7zip
[nugetimage]: https://img.shields.io/nuget/v/Cake.7zip.svg?logo=nuget&style=flat-square
[license]: LICENSE.txt
[standard-readme]: https://github.com/RichardLitt/standard-readme
[standard-readme compliant]: https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square
[documentation]: https://cake-contrib.github.io/Cake.7zip/
[api]: https://cakebuild.net/api/Cake.SevenZip/