// wwwroot/js/loginAnimation.js

window.initLoginAnimation = () => {
  const container = document.querySelector('.background-shapes');
  if (!container) return;

  const shapes = Array.from(container.children);
  const viewport = {
    width: window.innerWidth,
    height: window.innerHeight
  };

  // Asignar propiedades iniciales a cada forma
  const shapeProperties = shapes.map(shape => {
    const size = shape.offsetWidth;
    return {
      element: shape,
      x: Math.random() * (viewport.width - size),
      y: Math.random() * (viewport.height - size),
      vx: (Math.random() - 0.5) * 2, // Velocidad horizontal (-1 a 1)
      vy: (Math.random() - 0.5) * 2, // Velocidad vertical (-1 a 1)
      size: size
    };
  });

  function animate() {
    shapeProperties.forEach(prop => {
      // Mover la forma
      prop.x += prop.vx;
      prop.y += prop.vy;

      // Detección de colisión y rebote
      if (prop.x <= 0 || prop.x + prop.size >= viewport.width) {
        prop.vx *= -1; // Invertir dirección horizontal
      }
      if (prop.y <= 0 || prop.y + prop.size >= viewport.height) {
        prop.vy *= -1; // Invertir dirección vertical
      }

      // Aplicar la nueva posición
      prop.element.style.transform = `translate(${prop.x}px, ${prop.y}px)`;
    });

    requestAnimationFrame(animate);
  }

  // Iniciar la animación
  animate();

  // Actualizar el tamaño del viewport si la ventana cambia de tamaño
  window.addEventListener('resize', () => {
    viewport.width = window.innerWidth;
    viewport.height = window.innerHeight;
  });
};
