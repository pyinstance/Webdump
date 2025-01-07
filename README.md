# WebDumper - Webdump

WebDumper is a simple and effective command-line tool for downloading website content, including HTML and assets (images, scripts, and styles). It is designed to make it easy to download the source code of websites, along with their assets, for offline use or further analysis.

## Output
<img src="https://i.imgur.com/xXu2qJp.png">

## Features

- Download website HTML content.
- Download assets (images, CSS, JavaScript) referenced in the website.
- Recursively download linked pages from the same domain (with adjustable depth).
- Organized output folder structure for easier access to downloaded files.

## Installation

### Prerequisites

Before using WebDumper, ensure that you have the following installed:

- [.NET SDK](https://dotnet.microsoft.com/download) (version 5.0 or later)
- [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)

### Clone the Repository

To get started, clone the repository to your local machine.

```bash
git clone https://github.com/pyinstance/WebDumper.git
