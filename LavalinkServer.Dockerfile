FROM eclipse-temurin:17-jre-focal

WORKDIR /opt/Lavalink

COPY ./lib/Lavalink.jar Lavalink.jar
COPY ./lib/lavalink-conf.yml application.yml

EXPOSE 2333

CMD ["java", "-Xmx2G", "-jar", "Lavalink.jar"]