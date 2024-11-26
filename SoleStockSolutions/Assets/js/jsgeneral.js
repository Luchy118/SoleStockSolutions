class Popups {
    showErrorTimer(msg) {
        Swal.fire({
            icon: "error",
            scrollbarPadding: false,
            title: "¡Error!",
            html: msg,
            position: "top",
            timer: 3000,
            timerProgressBar: true,
            customClass: {
                confirmButton: 'custom-confirm-button'
            }
        });
    }

    showSuccessTimer(title, msg) {
        Swal.fire({
            icon: "success",
            scrollbarPadding: false,
            title: title,
            html: msg,
            position: "top",
            timer: 3000,
            timerProgressBar: true,
            customClass: {
                confirmButton: 'custom-confirm-button'
            }
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
}

const popups = new Popups();