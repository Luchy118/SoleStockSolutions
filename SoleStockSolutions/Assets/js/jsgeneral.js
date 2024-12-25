class Popups {
    showErrorTimer(msg) {
        Swal.fire({
            icon: "error",
            scrollbarPadding: false,
            title: "¡Error!",
            html: msg,
            position: "center",
            timer: 3000,
            timerProgressBar: true,
            customClass: {
                confirmButton: 'custom-confirm-button'
            }
        });
    }

    showSuccessTimer(title, msg, callback = null) {
        Swal.fire({
            icon: "success",
            scrollbarPadding: false,
            title: title,
            html: msg,
            position: "center",
            timer: 3000,
            timerProgressBar: true,
            customClass: {
                confirmButton: 'custom-confirm-button'
            }
        }).then(() => {
            if (callback)
                callback();
        });
    }

    showWarning(title, msg, callback = null) {
        Swal.fire({
            icon: "warning",
            scrollbarPadding: false,
            title: title,
            html: msg,
            position: "center",
            timerProgressBar: true,
            customClass: {
                confirmButton: 'custom-confirm-button'
            }
        }).then(() => {
            if (callback)
                callback();
        });
    }

    showConfirmLogOut() {
        Swal.fire({
            title: "Estás a punto de cerrar sesión",
            text: "¿Seguro que quieres continuar?",
            icon: "question",
            scrollbarPadding: false,
            showCancelButton: true,
            confirmButtonColor: "#3085d6",
            cancelButtonColor: "#d33",
            confirmButtonText: "SÍ",
            cancelButtonText: "NO"
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    type: "GET",
                    url: "/account/signout",
                    success: (response) => {
                        window.location.href = "/home/index";
                    },
                    error: (xhr, status, error) => {
                        console.log("Error en SignOut AJAX " + error);
                        this.showErrorTimer("Ha ocurrido un error. Por favor, inténtelo de nuevo.");
                    }
                });
            }
        });
    }

    showCopyLink() {
        let currentUrl = window.location.href;
        currentUrl = currentUrl.replace(/\/$|#$/, '');

        Swal.fire({
            title: "<strong>Copiar enlace del producto</strong>",
            html: `
                    <div style="display: flex; align-items: center;">
                        <input type="text" id="productLink" class="swal2-input" value="${currentUrl}" readonly style="flex: 1; font-size: 0.9em; margin: 0;">
                        <button id="copyButton" class="swal2-confirm swal2-styled" style="padding: 5px 15px; background-color: #b68a7b;">
                            <i class="fa fa-clone" aria-hidden="true"></i>
                        </button>
                    </div>`,
            showConfirmButton: false,
            didOpen: () => {
                const copyButton = document.getElementById('copyButton');
                const productLink = document.getElementById('productLink');
                productLink.blur();
                copyButton.addEventListener('click', () => {
                    productLink.select();
                    document.execCommand('copy');
                    Swal.fire({
                        icon: 'success',
                        title: 'Enlace copiado en el portapapeles',
                        timer: 1500,
                        showConfirmButton: false
                    });
                });
                copyButton.addEventListener('mouseover', () => {
                    copyButton.style.backgroundColor = '#a67a6b';
                });
                copyButton.addEventListener('mouseout', () => {
                    copyButton.style.backgroundColor = '#b68a7b';
                });
            }
        });
    }

    showConfirmDelete(title, msg, url, data, onSuccessScript = null) {
        Swal.fire({
            title: title,
            text: msg,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#3085d6",
            cancelButtonColor: "#d33",
            confirmButtonText: "Eliminar",
            cancelButtonText: "Cancelar"
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: url,
                    type: 'POST',
                    data: data,
                    success: function (response) {
                        if (response.success) {
                            Swal.fire({
                                title: "Eliminado",
                                text: "El registro ha sido eliminado correctamente.",
                                icon: "success"
                            }).then(() => {
                                if (onSuccessScript)
                                    eval(onSuccessScript);
                                else
                                    location.reload();
                            });
                        } else {
                            Swal.fire({
                                title: "Error",
                                text: "Hubo un problema al eliminar el registro.",
                                icon: "error"
                            });
                        }
                    },
                    error: function () {
                        Swal.fire({
                            title: "Error",
                            text: "Hubo un problema al eliminar el registro.",
                            icon: "error"
                        });
                    }
                });
            }
        });
    }

    showHTMLPopup(title, html, showButtons = true) {
        Swal.fire({
            title: title,
            html: html,
            showCloseButton: true,
            showCancelButton: showButtons,
            showConfirmButton: showButtons,
            focusConfirm: false,
            confirmButtonText: "Guardar",
            cancelButtonText: "Cancelar"
        });
    }
}

const popups = new Popups();