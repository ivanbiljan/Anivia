FROM eclipse-temurin:18-jre-focal

WORKDIR /opt/Lavalink

COPY ./lib/Lavalink.jar Lavalink.jar

EXPOSE 8080

CMD ["java", "-Djdk.tls.client.protocols=TLSv1.1,TLSv1.2", "-Xmx2G", "-jar", "Lavalink.jar"]