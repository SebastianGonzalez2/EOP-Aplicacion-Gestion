# ¿Qué estamos probando acá?

Esta carpeta tiene una pequeña aplicación de consola, separada de EOP. La usamos para probar si podemos enviar un relevamiento de obra a OpenAI y recibir conceptos de presupuesto estructurados, como materiales, mano de obra o traslados, con cantidades y unidades cuando están indicadas.

- `Program.cs`: el experimento con un relevamiento ficticio y las reglas de respuesta.
- `EOP.PruebaConcepto.IA.csproj`: la configuración del proyecto .NET y su dependencia de OpenAI.

No forma parte de la solución principal ni es una funcionalidad de producción terminada. Para ejecutarla hace falta una clave válida en la variable de entorno `OPENAI_API_KEY`, nunca guardada en Git. Las llamadas reales pueden consumir créditos.
