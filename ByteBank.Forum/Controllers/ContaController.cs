using ByteBank.Forum.Models;
using ByteBank.Forum.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ByteBank.Forum.Controllers
{
    public class ContaController : Controller
    {
        private UserManager<UsuarioAplicacao> _userManager;

        public UserManager<UsuarioAplicacao> UserManager
        {
            get
            {
                if (_userManager == null)
                {
                    var contextOwin = HttpContext.GetOwinContext();
                    _userManager = contextOwin.GetUserManager<UserManager<UsuarioAplicacao>>();
                }

                return _userManager;
            }

            set { _userManager = value; }
        }

        public ActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Registrar(ContaRegistrarViewModel contaRegistrarViewModel)
        {
            if (ModelState.IsValid)
            {
                var novoUsuario = new UsuarioAplicacao
                {
                    Email = contaRegistrarViewModel.Email,
                    UserName = contaRegistrarViewModel.UserName,
                    NomeCompleto = contaRegistrarViewModel.NomeCompleto
                };

                var usuarioCadastrado = await UserManager.FindByEmailAsync(contaRegistrarViewModel.Email);

                if (usuarioCadastrado != null)
                {
                    return View("AguardandoConfirmacao");
                }

                var resultado = await UserManager.CreateAsync(novoUsuario, contaRegistrarViewModel.Senha);

                if (!resultado.Succeeded)
                {
                    AdicionaErros(resultado);

                    return View(contaRegistrarViewModel);
                }

                // Enviar o email de confirmação
                await EnviarEmailDeConfirmacaoAsync(novoUsuario);

                return View("AguardandoConfirmacao");
            }

            return View(contaRegistrarViewModel);
        }

        public async Task<ActionResult> ConfirmacaoEmail(string usuarioId, string token)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(token))
            {
                return View("Error");
            }

            var resultado = await UserManager.ConfirmEmailAsync(usuarioId, token);

            if (!resultado.Succeeded)
            {
                return View("Error");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task EnviarEmailDeConfirmacaoAsync(UsuarioAplicacao usuarioAplicacaoModel)
        {
            var tokenEmail = await UserManager.GenerateEmailConfirmationTokenAsync(usuarioAplicacaoModel.Id);

            var linkDeCallback = Url.Action(
                "ConfirmacaoEmail",
                "Conta",
                new { usuarioId = usuarioAplicacaoModel.Id, token = tokenEmail },
                Request.Url.Scheme);

            await UserManager.SendEmailAsync(
                usuarioAplicacaoModel.Id,
                "Fórum ByteBank - Confirmação de Email",
                $"Bem-vido ao fórum ByteBank, cliqque aqui {linkDeCallback} para confirmar seu email!");
        }

        private void AdicionaErros(IdentityResult identityResult)
        {
            foreach (var erro in identityResult.Errors)
            {
                ModelState.AddModelError("", erro);
            }
        }
    }
}