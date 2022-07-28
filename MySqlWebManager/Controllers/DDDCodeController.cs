using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Text;

namespace MySqlWebManager.Controllers
{
    public class DDDCodeController : Controller
    {
        private readonly IFileProvider _fileProvider;

        public DDDCodeController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }
        public async Task<IActionResult> Index()
        {
            IDirectoryContents contents = _fileProvider.GetDirectoryContents("");
            var fileInfoList = contents.Where(f => f.IsDirectory == false).ToList();
            for (int i = 0; i < fileInfoList.Count(); i++)
            {
                var templateContent = await System.IO.File.ReadAllTextAsync(fileInfoList[i].PhysicalPath, Encoding.UTF8);
            }

            return View(contents);
        }

        //public async Task<IActionResult> GenerateCode()
        //{
        //    //IDirectoryContents contents= _fileProvider.GetDirectoryContents("*.txt");

        //    _fileProvider.

        //}

    }
}