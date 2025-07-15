pipeline {
    agent any
    
    parameters {
        string(name: 'IMAGE_TAG', defaultValue: 'latest', description: 'Docker image tag to deploy')
        string(name: 'BRANCH', defaultValue: 'main', description: 'Branch being deployed')
    }
    
    environment {
        REGISTRY = 'ghcr.io'
        IMAGE_NAME = 'your-github-username/just_climb'
        CONTAINER_NAME = 'just-climb-server'
        APP_PORT = '5000'
        
        // AWS 환경 변수
        ASPNETCORE_ENVIRONMENT = 'AWS'
        
        // PostgreSQL 설정
        DB_HOST = 'justclimb-postgres.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com'
        DB_PORT = '5432'
        DB_NAME = 'justclimb'
        
        // Redis 설정
        REDIS_HOST = 'justclimb-redis-kp9dum.serverless.apn2.cache.amazonaws.com'
        REDIS_PORT = '6379'
    }
    
    stages {
        stage('Preparation') {
            steps {
                script {
                    echo "🚀 Starting deployment of Just Climb Server"
                    echo "📦 Image: ${env.REGISTRY}/${env.IMAGE_NAME}:${params.IMAGE_TAG}"
                    echo "🌿 Branch: ${params.BRANCH}"
                    echo "🏗️ Environment: ${env.ASPNETCORE_ENVIRONMENT}"
                }
            }
        }
        
        stage('Stop Previous Container') {
            steps {
                script {
                    echo "🛑 Stopping previous container..."
                    sh '''
                        if [ $(docker ps -q -f name=${CONTAINER_NAME}) ]; then
                            docker stop ${CONTAINER_NAME}
                            echo "Previous container stopped"
                        else
                            echo "No previous container found"
                        fi
                        
                        if [ $(docker ps -aq -f name=${CONTAINER_NAME}) ]; then
                            docker rm ${CONTAINER_NAME}
                            echo "Previous container removed"
                        fi
                    '''
                }
            }
        }
        
        stage('Pull Docker Image') {
            steps {
                script {
                    echo "📥 Pulling Docker image..."
                    sh '''
                        docker pull ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}
                        echo "Docker image pulled successfully"
                    '''
                }
            }
        }
        
        stage('Deploy Application') {
            steps {
                script {
                    echo "🚀 Deploying Just Climb Server..."
                    sh '''
                        docker run -d \
                            --name ${CONTAINER_NAME} \
                            --restart unless-stopped \
                            -p ${APP_PORT}:5000 \
                            -e ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
                            -e ASPNETCORE_URLS=http://+:5000 \
                            -e DB_HOST=${DB_HOST} \
                            -e DB_PORT=${DB_PORT} \
                            -e DB_NAME=${DB_NAME} \
                            -e DB_USER=${DB_USER} \
                            -e DB_PASSWORD=${DB_PASSWORD} \
                            -e REDIS_HOST=${REDIS_HOST} \
                            -e REDIS_PORT=${REDIS_PORT} \
                            ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}
                    '''
                }
            }
        }
        
        stage('Health Check') {
            steps {
                script {
                    echo "🏥 Performing health check..."
                    sh '''
                        # Wait for application to start
                        echo "Waiting for application to start..."
                        sleep 30
                        
                        # Health check with retry
                        for i in {1..10}; do
                            if curl -f http://localhost:${APP_PORT}/api/v1/health; then
                                echo "✅ Health check passed"
                                break
                            else
                                echo "❌ Health check failed, attempt $i/10"
                                sleep 10
                            fi
                            
                            if [ $i -eq 10 ]; then
                                echo "❌ Health check failed after 10 attempts"
                                exit 1
                            fi
                        done
                    '''
                }
            }
        }
        
        stage('Database Migration') {
            steps {
                script {
                    echo "🗄️ Running database migrations..."
                    sh '''
                        docker exec ${CONTAINER_NAME} \
                            dotnet ef database update \
                            --project /app \
                            --environment ${ASPNETCORE_ENVIRONMENT} \
                            --verbose
                    '''
                }
            }
        }
        
        stage('Cleanup') {
            steps {
                script {
                    echo "🧹 Cleaning up old images..."
                    sh '''
                        # Remove old images (keep last 3)
                        docker images ${REGISTRY}/${IMAGE_NAME} \
                            --format "table {{.Repository}}:{{.Tag}}\t{{.CreatedAt}}" \
                            | tail -n +2 \
                            | sort -k2 -r \
                            | tail -n +4 \
                            | awk '{print $1}' \
                            | xargs -r docker rmi || true
                    '''
                }
            }
        }
    }
    
    post {
        success {
            echo "✅ Just Climb Server deployed successfully!"
            echo "🌐 Application URL: http://localhost:${env.APP_PORT}"
            echo "🏥 Health Check: http://localhost:${env.APP_PORT}/api/v1/health"
        }
        
        failure {
            echo "❌ Deployment failed!"
            echo "📋 Check Jenkins logs for details"
            
            // Rollback on failure
            script {
                sh '''
                    echo "🔄 Rolling back to previous version..."
                    if [ $(docker ps -q -f name=${CONTAINER_NAME}) ]; then
                        docker stop ${CONTAINER_NAME}
                        docker rm ${CONTAINER_NAME}
                    fi
                    
                    # Try to start with latest stable image
                    docker run -d \
                        --name ${CONTAINER_NAME} \
                        --restart unless-stopped \
                        -p ${APP_PORT}:5000 \
                        -e ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
                        -e ASPNETCORE_URLS=http://+:5000 \
                        ${REGISTRY}/${IMAGE_NAME}:latest || true
                '''
            }
        }
        
        always {
            echo "🏁 Deployment pipeline completed"
            
            // Display container logs
            script {
                sh '''
                    echo "📜 Container logs:"
                    docker logs ${CONTAINER_NAME} --tail=50 || true
                '''
            }
        }
    }
} 